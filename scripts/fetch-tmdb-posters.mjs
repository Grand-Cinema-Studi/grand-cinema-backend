#!/usr/bin/env node
// Fetches from TMDB the poster, release date and running time of every demo movie of docs/database/seed-dev.sql,
// then writes docs/database/seed-posters.sql (UPDATE statements, applied after the seed by reset-demo-screenings.sh).
//
// Each movie line of seed-dev.sql ends with a comment "-- tmdb: <original title> (<year>)", used for the search.
// The credential is read from the TMDB_TOKEN environment variable, never from the command line nor from Git:
// put TMDB_TOKEN=<value> in the .env file of the repository (ignored by Git). Both TMDB credentials are accepted:
// the "API Read Access Token" (long, starts with eyJ) or the "API Key" (32 characters),
// both found in TMDB account > Settings > API.
//
// Usage, from the root of the repository (Node 20.6 or later):
//   node --env-file=.env scripts/fetch-tmdb-posters.mjs
//
// This product uses the TMDB API but is not endorsed or certified by TMDB.

import { readFile, writeFile } from 'node:fs/promises';

const token = (process.env.TMDB_TOKEN ?? '').trim().replace(/^["']|["']$/g, '');
if (!token) {
  console.error('TMDB_TOKEN is missing: add TMDB_TOKEN=<token> to .env, then run node --env-file=.env scripts/fetch-tmdb-posters.mjs');
  process.exit(1);
}
// The API Key (v3) goes in the query string; the API Read Access Token goes in the Authorization header.
const isApiKey = /^[0-9a-f]{32}$/i.test(token);

const seedUrl = new URL('../docs/database/seed-dev.sql', import.meta.url);
const outputUrl = new URL('../docs/database/seed-posters.sql', import.meta.url);
const imageBase = 'https://image.tmdb.org/t/p/w500';

async function tmdb(path, params = {}) {
  const url = new URL(`https://api.themoviedb.org/3${path}`);
  for (const [key, value] of Object.entries(params)) url.searchParams.set(key, String(value));
  if (isApiKey) url.searchParams.set('api_key', token);
  const headers = isApiKey ? { Accept: 'application/json' } : { Authorization: `Bearer ${token}`, Accept: 'application/json' };
  const response = await fetch(url, { headers });
  if (!response.ok) {
    const body = await response.json().catch(() => ({}));
    const kind = isApiKey ? 'API Key' : 'API Read Access Token';
    throw new Error(`TMDB ${response.status} on ${url.pathname} with the ${kind} (${token.length} characters): ${body.status_message ?? response.statusText}`);
  }
  return response.json();
}

async function findMovie(title, year) {
  const withYear = await tmdb('/search/movie', { query: title, primary_release_year: year, language: 'fr-FR', include_adult: false });
  if (withYear.results.length > 0) return withYear.results[0];
  const anyYear = await tmdb('/search/movie', { query: title, language: 'fr-FR', include_adult: false });
  return anyYear.results[0];
}

const sqlText = (value) => `'${String(value).replaceAll("'", "''")}'`;

// A movie line of seed-dev.sql: ('<id>', '<title>', ...), -- tmdb: <original title> (<year>)
// Parsed in two short anchored steps rather than one regular expression spanning the whole line:
// two wildcards in the same pattern backtrack in quadratic time on a long line.
const TMDB_TAG = '-- tmdb: ';
const movieStart = /^\s*\('([0-9a-f-]{36})', '((?:[^']|'')*)',/;
const tagYear = /\((\d{4})\)$/;

function parseMovieLine(line) {
  const tagIndex = line.lastIndexOf(TMDB_TAG);
  if (tagIndex === -1) return undefined;
  const start = movieStart.exec(line);
  const tag = line.slice(tagIndex + TMDB_TAG.length).trim();
  const year = tagYear.exec(tag);
  if (!start || !year) return undefined;
  return {
    id: start[1],
    title: start[2].replaceAll("''", "'"),
    original: tag.slice(0, year.index).trim(),
    year: Number(year[1]),
  };
}

try {
const seed = await readFile(seedUrl, 'utf8');
const movies = seed.split('\n').map(parseMovieLine).filter((movie) => movie !== undefined);
if (movies.length === 0) throw new Error('No movie line found in seed-dev.sql.');

const updates = [];
const warnings = [];
for (const movie of movies) {
  const found = await findMovie(movie.original, movie.year);
  if (!found) {
    warnings.push(`NOT FOUND   ${movie.title} (${movie.year})`);
    continue;
  }
  const details = await tmdb(`/movie/${found.id}`, { language: 'fr-FR' });
  const posterPath = details.poster_path ?? found.poster_path;
  const releaseDate = details.release_date || found.release_date;
  const runtime = details.runtime;

  const set = [];
  if (posterPath) set.push(`"PosterUrl" = ${sqlText(imageBase + posterPath)}`);
  else warnings.push(`NO POSTER   ${movie.title} (TMDB ${found.id})`);
  if (releaseDate) set.push(`"ReleaseDate" = ${sqlText(releaseDate)}`);
  if (runtime) set.push(`"DurationMinutes" = ${Number(runtime)}`);

  const tmdbYear = releaseDate ? Number(releaseDate.slice(0, 4)) : undefined;
  if (tmdbYear !== movie.year) warnings.push(`CHECK YEAR  ${movie.title}: seed ${movie.year}, TMDB "${details.title}" ${tmdbYear ?? '?'}`);

  if (set.length > 0) {
    updates.push(`UPDATE "Movies" SET ${set.join(', ')} WHERE "Id" = ${sqlText(movie.id)}; -- ${movie.title} -> TMDB ${found.id}`);
  }
  console.log(`${posterPath ? 'ok ' : '-- '} ${movie.title} -> ${details.title} (${releaseDate ?? '?'}, ${runtime ?? '?'} min)`);
}

const header = [
  '-- Generated by scripts/fetch-tmdb-posters.mjs: do not edit by hand, run the script again.',
  `-- ${new Date().toISOString().slice(0, 10)}, ${updates.length} movies updated from TMDB (poster, release date, running time).`,
  '-- This product uses the TMDB API but is not endorsed or certified by TMDB. Posters: https://www.themoviedb.org',
  '',
];
await writeFile(outputUrl, header.concat(updates, '').join('\n'), 'utf8');

console.log(`\n${updates.length}/${movies.length} movies written to docs/database/seed-posters.sql`);
if (warnings.length > 0) {
  console.log('\nTo check by hand (the search may have picked another movie):');
  for (const warning of warnings) console.log(`  ${warning}`);
}
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
}

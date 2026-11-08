namespace GrandCinema.Application.Abstractions.Storage;

public sealed record class PosterFormat(string ContentType, string Extension)
{
    public static readonly PosterFormat Jpeg = new("image/jpeg", ".jpg");
    public static readonly PosterFormat Png = new("image/png", ".png");
    public static readonly PosterFormat Webp = new("image/webp", ".webp");
}

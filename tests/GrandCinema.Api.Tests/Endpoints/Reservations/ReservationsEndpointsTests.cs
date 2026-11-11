using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GrandCinema.Domain.Entities;
using GrandCinema.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace GrandCinema.Api.Tests.Endpoints.Reservations;

[Collection(ApiCollection.Name)]
public class ReservationsEndpointsTests
{
    private readonly GrandCinemaApiFactory _api;

    public ReservationsEndpointsTests(GrandCinemaApiFactory api)
    {
        _api = api;
    }

    // A screening on a day no other test uses, in its own room of seatCount seats (row A)
    private async Task<Guid> ScreeningWithSeatsAsync(int seatCount, DateTime startsAt)
    {
        var room = new Room { Id = Guid.NewGuid(), Name = $"Room {Guid.NewGuid():N}", Capacity = seatCount };
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Reservations contract", Synopsis = "Synopsis", DurationMinutes = 110, Genre = "Drama", ReleaseDate = new DateOnly(2030, 1, 1), PosterUrl = "https://example.com/poster.jpg" };
        var screening = new Screening { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = startsAt, Price = 9.5m };
        await using var db = _api.CreateDbContext();
        db.Seats.AddRange(Enumerable.Range(1, seatCount).Select(number => new Seat { Id = Guid.NewGuid(), Room = room, Row = "A", Number = number }));
        db.Screenings.Add(screening);
        await db.SaveChangesAsync();
        return screening.Id;
    }

    private async Task<HttpClient> SignedInCustomerAsync()
    {
        var client = _api.CreateVisitorClient();
        var token = await GrandCinemaApiFactory.RegisterCustomerAsync(client, GrandCinemaApiFactory.NewEmail());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // A hold of all the seats of a new screening, as the front makes it: the client of its owner (to dispose), its address, its identifier
    private async Task<(HttpClient Client, Uri Location, string ReservationId)> HoldAsync(int seatCount, DateTime startsAt)
    {
        var screeningId = await ScreeningWithSeatsAsync(seatCount, startsAt);
        var client = await SignedInCustomerAsync();
        var created = await client.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount });
        created.EnsureSuccessStatusCode();
        var hold = await created.Content.ReadFromJsonAsync<JsonElement>();
        return (client, created.Headers.Location!, hold.GetProperty("reservationId").GetString()!);
    }

    // What Stripe sends: no access token, the signature in the Stripe-Signature header
    private static HttpRequestMessage StripeNotification(string payload, string signature) =>
        new(HttpMethod.Post, "/api/payments/stripe-webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            Headers = { { "Stripe-Signature", signature } }
        };

    // A ticket bought and paid for a new screening of seatCount seats: its screening and its code
    private async Task<(Guid ScreeningId, string TicketCode)> PaidTicketAsync(int seatCount, DateTime startsAt)
    {
        var screeningId = await ScreeningWithSeatsAsync(seatCount, startsAt);
        using var customer = await SignedInCustomerAsync();
        var created = await customer.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount });
        created.EnsureSuccessStatusCode();
        var reservationId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("reservationId").GetString()!;
        using var stripe = _api.CreateVisitorClient();
        (await stripe.SendAsync(StripeNotification(reservationId, FakePaymentGateway.ValidSignature))).EnsureSuccessStatusCode();
        var ticket = await customer.GetFromJsonAsync<JsonElement>($"{created.Headers.Location}/ticket");
        return (screeningId, ticket.GetProperty("ticketCode").GetString()!);
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_the_visitor_is_not_signed_in_Then_401()
    {
        // Arrange
        var screeningId = await ScreeningWithSeatsAsync(4, new DateTime(2031, 5, 20, 20, 30, 0, DateTimeKind.Unspecified));
        using var client = _api.CreateVisitorClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount = 2 });

        // Assert: the front then invites the visitor to sign in or to create an account
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_a_customer_holds_two_seats_Then_201_and_the_seats_are_held_for_10_minutes()
    {
        // Arrange: 4 seats, nothing sold
        var screeningId = await ScreeningWithSeatsAsync(4, new DateTime(2031, 5, 21, 20, 30, 0, DateTimeKind.Unspecified));
        using var client = await SignedInCustomerAsync();
        var before = DateTime.UtcNow;

        // Act
        var response = await client.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount = 2 });

        // Assert: a hold waiting for payment, not a sale (criterion 3)
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var hold = await response.Content.ReadFromJsonAsync<JsonElement>();
        var reservationId = hold.GetProperty("reservationId").GetString();
        response.Headers.Location?.ToString().ShouldBe($"/api/reservations/{reservationId}");
        hold.GetProperty("status").GetString().ShouldBe("PendingPayment");
        // The movie of the screening: the front goes back to its page
        await using (var db = _api.CreateDbContext())
        {
            var movieId = (await db.Screenings.SingleAsync(s => s.Id == screeningId)).MovieId;
            hold.GetProperty("movieId").GetGuid().ShouldBe(movieId);
        }
        hold.GetProperty("seats").EnumerateArray().Select(seat => seat.GetString()).ShouldBe(new[] { "A1", "A2" });
        hold.GetProperty("totalAmount").GetDecimal().ShouldBe(19m);
        // An instant in UTC ("Z"), 10 minutes after the request
        var expiresAt = hold.GetProperty("expiresAt").GetString()!;
        expiresAt.ShouldEndWith("Z");
        DateTime.Parse(expiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            .ShouldBeInRange(before.AddMinutes(10), DateTime.UtcNow.AddMinutes(10));
        // The counter of the schedule is updated at once (US 2.2): 4 seats, 2 held
        var schedule = await client.GetFromJsonAsync<JsonElement>("/api/schedule?date=2031-05-21");
        schedule.GetProperty("movies")[0].GetProperty("screenings")[0].GetProperty("remainingSeats").GetInt32().ShouldBe(2);
        // The owner can read the hold again (payment page, US 3.1)
        (await client.GetAsync(response.Headers.Location)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_more_seats_are_asked_than_remain_Then_409_not_enough_seats()
    {
        // Arrange
        var screeningId = await ScreeningWithSeatsAsync(2, new DateTime(2031, 5, 22, 20, 30, 0, DateTimeKind.Unspecified));
        using var client = await SignedInCustomerAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount = 3 });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("type").GetString().ShouldBe("/errors/not-enough-seats");
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_two_users_book_the_last_seat_at_the_same_time_Then_only_one_succeeds()
    {
        // Arrange: bug 0001, a screening with a single seat and two signed-in customers
        var screeningId = await ScreeningWithSeatsAsync(1, new DateTime(2031, 5, 23, 20, 30, 0, DateTimeKind.Unspecified));
        using var first = await SignedInCustomerAsync();
        using var second = await SignedInCustomerAsync();

        // Act: both requests leave at the same time
        var responses = await Task.WhenAll(
            first.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount = 1 }),
            second.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount = 1 }));

        // Assert: one hold and one explicit refusal, never two customers for one seat
        responses.Select(response => response.StatusCode).OrderBy(status => status)
            .ShouldBe(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict });
        var refused = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        var problem = await refused.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("type").GetString().ShouldBe("/errors/not-enough-seats");
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_another_customer_asks_for_the_reservation_Then_404()
    {
        // Arrange: a hold of one customer
        var screeningId = await ScreeningWithSeatsAsync(2, new DateTime(2031, 5, 24, 20, 30, 0, DateTimeKind.Unspecified));
        using var owner = await SignedInCustomerAsync();
        using var other = await SignedInCustomerAsync();
        var created = await owner.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount = 1 });
        created.EnsureSuccessStatusCode();

        // Act
        var response = await other.GetAsync(created.Headers.Location);

        // Assert: the same answer as an unknown reservation, its existence is not revealed (OWASP A01)
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("type").GetString().ShouldBe("/errors/reservation-not-found");
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_the_owner_cancels_the_hold_Then_204_and_the_seats_are_on_sale_again_at_once()
    {
        // Arrange: the 2 seats of the screening are held
        var screeningId = await ScreeningWithSeatsAsync(2, new DateTime(2031, 5, 26, 20, 30, 0, DateTimeKind.Unspecified));
        using var client = await SignedInCustomerAsync();
        var created = await client.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount = 2 });
        created.EnsureSuccessStatusCode();

        // Act
        var response = await client.DeleteAsync(created.Headers.Location);

        // Assert: without waiting for the 10 minutes
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var schedule = await client.GetFromJsonAsync<JsonElement>("/api/schedule?date=2031-05-26");
        schedule.GetProperty("movies")[0].GetProperty("screenings")[0].GetProperty("remainingSeats").GetInt32().ShouldBe(2);
        var hold = await client.GetFromJsonAsync<JsonElement>(created.Headers.Location);
        hold.GetProperty("status").GetString().ShouldBe("Cancelled");
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_another_customer_cancels_the_hold_Then_404_and_the_seats_stay_held()
    {
        // Arrange
        var screeningId = await ScreeningWithSeatsAsync(2, new DateTime(2031, 5, 27, 20, 30, 0, DateTimeKind.Unspecified));
        using var owner = await SignedInCustomerAsync();
        using var other = await SignedInCustomerAsync();
        var created = await owner.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount = 1 });
        created.EnsureSuccessStatusCode();

        // Act
        var response = await other.DeleteAsync(created.Headers.Location);

        // Assert: same answer as an unknown reservation, and the hold of the owner is untouched
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var hold = await owner.GetFromJsonAsync<JsonElement>(created.Headers.Location);
        hold.GetProperty("status").GetString().ShouldBe("PendingPayment");
    }

    [Fact]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    public async Task When_a_customer_holds_more_than_10_seats_of_a_screening_in_several_holds_Then_409_seat_limit_reached()
    {
        // Arrange: 20 seats; the customer already holds 10 of them (as from a first tab)
        var screeningId = await ScreeningWithSeatsAsync(20, new DateTime(2031, 5, 28, 20, 30, 0, DateTimeKind.Unspecified));
        using var client = await SignedInCustomerAsync();
        var first = await client.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount = 10 });
        first.EnsureSuccessStatusCode();

        // Act: one more seat, as from a second tab
        var response = await client.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount = 1 });

        // Assert: refused although 10 seats are still free
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("type").GetString().ShouldBe("/errors/seat-limit-reached");
    }

    [Fact]
    [Trait("US", "3.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_customer_accepts_the_terms_of_sale_Then_200_and_the_address_of_the_payment_page()
    {
        // Arrange
        var hold = await HoldAsync(2, new DateTime(2031, 5, 29, 20, 30, 0, DateTimeKind.Unspecified));
        using var client = hold.Client;

        // Act
        var response = await client.PostAsJsonAsync($"{hold.Location}/payment", new { acceptTermsOfSale = true });

        // Assert: the front sends the customer to this page, where the card is typed
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<JsonElement>();
        page.GetProperty("checkoutUrl").GetString().ShouldBe($"https://checkout.example/{hold.ReservationId}");
    }

    [Fact]
    [Trait("US", "3.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_terms_of_sale_are_not_accepted_Then_400()
    {
        // Arrange
        var hold = await HoldAsync(2, new DateTime(2031, 5, 30, 20, 30, 0, DateTimeKind.Unspecified));
        using var client = hold.Client;

        // Act
        var response = await client.PostAsJsonAsync($"{hold.Location}/payment", new { acceptTermsOfSale = false });

        // Assert: criterion 4, the field in error is named for the front
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("errors").TryGetProperty("acceptTermsOfSale", out _).ShouldBeTrue();
    }

    [Fact]
    [Trait("US", "3.1")]
    [Trait("Category", "Integration")]
    public async Task When_stripe_notifies_the_payment_Then_the_reservation_is_confirmed_and_its_seats_stay_sold()
    {
        // Arrange: the 2 seats of the screening are held
        var hold = await HoldAsync(2, new DateTime(2031, 5, 31, 20, 30, 0, DateTimeKind.Unspecified));
        using var client = hold.Client;
        using var stripe = _api.CreateVisitorClient();

        // Act
        var response = await stripe.SendAsync(StripeNotification(hold.ReservationId, FakePaymentGateway.ValidSignature));

        // Assert: criterion 1, the reservation is recorded and its seats are sold
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var reservation = await client.GetFromJsonAsync<JsonElement>(hold.Location);
        reservation.GetProperty("status").GetString().ShouldBe("Confirmed");
        var schedule = await client.GetFromJsonAsync<JsonElement>("/api/schedule?date=2031-05-31");
        schedule.GetProperty("movies")[0].GetProperty("screenings")[0].GetProperty("remainingSeats").GetInt32().ShouldBe(0);
    }

    [Fact]
    [Trait("US", "3.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_notification_is_not_signed_by_stripe_Then_400_and_nothing_is_confirmed()
    {
        // Arrange
        var hold = await HoldAsync(1, new DateTime(2031, 6, 1, 20, 30, 0, DateTimeKind.Unspecified));
        using var client = hold.Client;
        using var attacker = _api.CreateVisitorClient();

        // Act: a forged notification, without the signature of Stripe
        var response = await attacker.SendAsync(StripeNotification(hold.ReservationId, "t=1,v1=forged"));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var reservation = await client.GetFromJsonAsync<JsonElement>(hold.Location);
        reservation.GetProperty("status").GetString().ShouldBe("PendingPayment");
    }

    [Fact]
    [Trait("US", "3.2")]
    [Trait("Category", "Integration")]
    public async Task When_a_paid_reservation_is_read_Then_its_ticket_shows_the_movie_the_screening_the_room_the_seats_and_a_code()
    {
        // Arrange: 2 seats held, then paid
        var hold = await HoldAsync(2, new DateTime(2032, 6, 1, 20, 30, 0, DateTimeKind.Unspecified));
        using var client = hold.Client;
        using var stripe = _api.CreateVisitorClient();
        (await stripe.SendAsync(StripeNotification(hold.ReservationId, FakePaymentGateway.ValidSignature))).EnsureSuccessStatusCode();

        // Act
        var response = await client.GetAsync($"{hold.Location}/ticket");

        // Assert: criterion 1, and the names the front relies on
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var ticket = await response.Content.ReadFromJsonAsync<JsonElement>();
        ticket.GetProperty("reservationId").GetString().ShouldBe(hold.ReservationId);
        // ShouldNotBeNull tells the compiler that the code is not null below
        var ticketCode = ticket.GetProperty("ticketCode").GetString();
        ticketCode.ShouldNotBeNull();
        ticketCode.ShouldMatch("^[0-9a-f]{32}$");
        ticket.GetProperty("movieTitle").GetString().ShouldBe("Reservations contract");
        ticket.GetProperty("startsAt").GetString().ShouldBe("2032-06-01T20:30:00");
        ticket.GetProperty("roomName").GetString().ShouldStartWith("Room ");
        ticket.GetProperty("seats").EnumerateArray().Select(seat => seat.GetString()).ShouldBe(new[] { "A1", "A2" });
        ticket.GetProperty("screeningCancelled").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    [Trait("US", "3.2")]
    [Trait("Category", "Integration")]
    public async Task When_the_reservation_is_not_paid_Then_it_has_no_ticket()
    {
        // Arrange: held, never paid (or refused by the bank: Stripe sends nothing)
        var hold = await HoldAsync(1, new DateTime(2032, 6, 2, 20, 30, 0, DateTimeKind.Unspecified));
        using var client = hold.Client;

        // Act
        var response = await client.GetAsync($"{hold.Location}/ticket");

        // Assert: criterion 3
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("type").GetString().ShouldBe("/errors/ticket-not-found");
    }

    [Fact]
    [Trait("US", "3.2")]
    [Trait("Category", "Integration")]
    public async Task When_a_customer_lists_their_tickets_Then_only_their_paid_reservations_are_listed()
    {
        // Arrange: the customer pays one reservation, holds another without paying; another customer pays one
        var paid = await HoldAsync(2, new DateTime(2032, 6, 3, 20, 30, 0, DateTimeKind.Unspecified));
        using var client = paid.Client;
        var otherScreeningId = await ScreeningWithSeatsAsync(1, new DateTime(2032, 6, 4, 20, 30, 0, DateTimeKind.Unspecified));
        (await client.PostAsJsonAsync("/api/reservations", new { screeningId = otherScreeningId, seatCount = 1 })).EnsureSuccessStatusCode();
        var someoneElse = await HoldAsync(1, new DateTime(2032, 6, 5, 20, 30, 0, DateTimeKind.Unspecified));
        someoneElse.Client.Dispose();
        using var stripe = _api.CreateVisitorClient();
        (await stripe.SendAsync(StripeNotification(paid.ReservationId, FakePaymentGateway.ValidSignature))).EnsureSuccessStatusCode();
        (await stripe.SendAsync(StripeNotification(someoneElse.ReservationId, FakePaymentGateway.ValidSignature))).EnsureSuccessStatusCode();

        // Act
        var response = await client.GetAsync("/api/users/me/tickets");

        // Assert: criterion 2
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tickets = await response.Content.ReadFromJsonAsync<JsonElement>();
        var ticket = tickets.EnumerateArray().ShouldHaveSingleItem();
        ticket.GetProperty("reservationId").GetString().ShouldBe(paid.ReservationId);
        ticket.GetProperty("movieTitle").GetString().ShouldBe("Reservations contract");
        ticket.GetProperty("startsAt").GetString().ShouldBe("2032-06-03T20:30:00");
        ticket.GetProperty("seatCount").GetInt32().ShouldBe(2);
        ticket.GetProperty("screeningCancelled").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    [Trait("US", "4.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_inspector_scans_a_valid_ticket_Then_access_is_granted_and_a_second_scan_is_refused()
    {
        // Arrange: a ticket of 2 seats, paid
        var (screeningId, ticketCode) = await PaidTicketAsync(2, new DateTime(2032, 6, 10, 20, 30, 0, DateTimeKind.Unspecified));
        using var inspector = await _api.CreateClientWithRoleAsync(UserRole.TicketInspector);

        // Act
        var first = await inspector.PostAsJsonAsync("/api/ticket-checks", new { screeningId, ticketCode });
        var second = await inspector.PostAsJsonAsync("/api/ticket-checks", new { screeningId, ticketCode });

        // Assert: criterion 1, what the inspector sees; criterion 2, the explicit refusal
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var check = await first.Content.ReadFromJsonAsync<JsonElement>();
        check.GetProperty("movieTitle").GetString().ShouldBe("Reservations contract");
        check.GetProperty("startsAt").GetString().ShouldBe("2032-06-10T20:30:00");
        check.GetProperty("roomName").GetString().ShouldStartWith("Room ");
        check.GetProperty("seats").EnumerateArray().Select(seat => seat.GetString()).ShouldBe(new[] { "A1", "A2" });
        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("type").GetString().ShouldBe("/errors/ticket-already-used");
    }

    [Fact]
    [Trait("US", "4.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_inspector_scans_a_ticket_of_another_screening_or_an_unknown_code_Then_access_is_refused()
    {
        // Arrange: a ticket for 20:30; the inspector controls the 22:45 screening
        var (_, ticketCode) = await PaidTicketAsync(1, new DateTime(2032, 6, 11, 20, 30, 0, DateTimeKind.Unspecified));
        var otherScreeningId = await ScreeningWithSeatsAsync(1, new DateTime(2032, 6, 11, 22, 45, 0, DateTimeKind.Unspecified));
        using var inspector = await _api.CreateClientWithRoleAsync(UserRole.TicketInspector);

        // Act
        var wrongScreening = await inspector.PostAsJsonAsync("/api/ticket-checks", new { screeningId = otherScreeningId, ticketCode });
        var unknown = await inspector.PostAsJsonAsync("/api/ticket-checks", new { screeningId = otherScreeningId, ticketCode = Reservation.NewTicketCode() });
        var notATicket = await inspector.PostAsJsonAsync("/api/ticket-checks", new { screeningId = otherScreeningId, ticketCode = "https://example.com" });

        // Assert: criterion 3, one explicit refusal per case
        async Task<string?> TypeOf(HttpResponseMessage response) =>
            (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("type").GetString();
        wrongScreening.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await TypeOf(wrongScreening)).ShouldBe("/errors/ticket-for-another-screening");
        unknown.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await TypeOf(unknown)).ShouldBe("/errors/ticket-not-found");
        notATicket.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await TypeOf(notATicket)).ShouldBe("/errors/ticket-not-found");
    }

    [Fact]
    [Trait("US", "4.1")]
    [Trait("Category", "Integration")]
    public async Task When_the_inspector_opens_the_controls_Then_the_screenings_of_the_day_are_listed()
    {
        // Arrange: two screenings that day, the later one created first
        var late = await ScreeningWithSeatsAsync(1, new DateTime(2032, 6, 12, 22, 45, 0, DateTimeKind.Unspecified));
        var early = await ScreeningWithSeatsAsync(1, new DateTime(2032, 6, 12, 14, 0, 0, DateTimeKind.Unspecified));
        using var inspector = await _api.CreateClientWithRoleAsync(UserRole.TicketInspector);

        // Act
        var response = await inspector.GetAsync("/api/ticket-checks/screenings?date=2032-06-12");

        // Assert: by time, with what the inspector needs to recognise his door
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var screenings = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        screenings.Select(s => s.GetProperty("screeningId").GetGuid()).ShouldBe(new[] { early, late });
        screenings[0].GetProperty("movieTitle").GetString().ShouldBe("Reservations contract");
        screenings[0].GetProperty("startsAt").GetString().ShouldBe("2032-06-12T14:00:00");
        screenings[0].GetProperty("roomName").GetString().ShouldStartWith("Room ");
    }

    [Theory]
    [Trait("US", "4.1")]
    [Trait("Category", "Integration")]
    [InlineData(UserRole.Customer)]
    [InlineData(UserRole.Cashier)]
    [InlineData(UserRole.Manager)]
    public async Task When_another_role_checks_a_ticket_Then_403(UserRole role)
    {
        // Arrange
        var (screeningId, ticketCode) = await PaidTicketAsync(1, new DateTime(2032, 6, 13, 20, 30, 0, DateTimeKind.Unspecified).AddMinutes((int)role));
        using var client = await _api.CreateClientWithRoleAsync(role);

        // Act
        var response = await client.PostAsJsonAsync("/api/ticket-checks", new { screeningId, ticketCode });

        // Assert: only the inspector lets people in (Bloc 1)
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [Trait("US", "2.3")]
    [Trait("Category", "Integration")]
    [InlineData(UserRole.Cashier)]
    [InlineData(UserRole.TicketInspector)]
    [InlineData(UserRole.Manager)]
    public async Task When_a_staff_member_holds_seats_Then_403(UserRole role)
    {
        // Arrange: a screening with seats left, each case its own (one minute apart)
        var screeningId = await ScreeningWithSeatsAsync(2, new DateTime(2032, 6, 20, 20, 30, 0, DateTimeKind.Unspecified).AddMinutes((int)role));
        using var client = await _api.CreateClientWithRoleAsync(role);

        // Act
        var response = await client.PostAsJsonAsync("/api/reservations", new { screeningId, seatCount = 1 });

        // Assert: only a customer buys a ticket (decision of the Product Owner, 07/11/2026)
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}

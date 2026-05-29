using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using SEN_T_PAZAR.Controllers;
using SEN_T_PAZAR.Models;
using Xunit;

namespace SenTPazar.Tests;

public class ApiAccountControllerTests
{
    [Fact]
    public async Task Messages_ReturnsThreadedConversationList()
    {
        using var harness = new TestHarness();

        var owner = new ApplicationUser
        {
            UserName = "owner",
            Email = "owner@sent.com",
            FullName = "Owner User"
        };

        var createResult = await harness.UserManager.CreateAsync(owner, "Password1!");
        Assert.True(createResult.Succeeded);

        harness.Db.Listings.Add(new Listing
        {
            Id = 110,
            Title = "Test Listing",
            Category = "realestate",
            IsApproved = true,
            IsClosed = false,
            CreatedAt = DateTime.UtcNow
        });

        harness.Db.VisitorMessages.AddRange(
            new VisitorMessage
            {
                ListingId = 110,
                ConversationId = "conv-1",
                RecipientUserId = owner.Id,
                SenderName = "Visitor",
                SenderEmail = "visitor@mail.com",
                SenderRole = "visitor",
                Subject = "Soru",
                Message = "Merhaba",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-2)
            },
            new VisitorMessage
            {
                ListingId = 110,
                ConversationId = "conv-1",
                RecipientUserId = owner.Id,
                SenderUserId = owner.Id,
                SenderName = owner.FullName,
                SenderEmail = owner.Email!,
                SenderRole = "owner",
                Subject = "Re: Soru",
                Message = "Yanit",
                CreatedAtUtc = DateTime.UtcNow.AddMinutes(-1)
            });

        await harness.Db.SaveChangesAsync();

        var controller = harness.WithUser(
            new AccountControllerApi(
                harness.UserManager,
                harness.Configuration,
                harness.Db,
                new NoOpUserMessageAutomationService(),
                NullLogger<AccountControllerApi>.Instance),
            owner.Id);

        var action = await controller.Messages();
        var ok = Assert.IsType<OkObjectResult>(action);
        var threads = Assert.IsAssignableFrom<List<MobileMessageThreadDto>>(ok.Value);

        Assert.Single(threads);
        Assert.Equal("conv-1", threads[0].ConversationId);
        Assert.Equal(2, threads[0].Messages.Count);
        Assert.Equal("owner", threads[0].Messages[1].SenderRole);
    }

    [Fact]
    public async Task ReplyMessage_PersistsReplyAndMarksRootAsRead()
    {
        using var harness = new TestHarness();

        var owner = new ApplicationUser
        {
            UserName = "owner2",
            Email = "owner2@sent.com",
            FullName = "Owner Two"
        };

        var createResult = await harness.UserManager.CreateAsync(owner, "Password1!");
        Assert.True(createResult.Succeeded);

        harness.Db.Listings.Add(new Listing
        {
            Id = 120,
            Title = "Reply Listing",
            Category = "realestate",
            IsApproved = true,
            IsClosed = false,
            CreatedAt = DateTime.UtcNow
        });

        var root = new VisitorMessage
        {
            ListingId = 120,
            ConversationId = "conv-reply",
            RecipientUserId = owner.Id,
            SenderName = "Visitor",
            SenderEmail = "visitor2@mail.com",
            SenderPhone = "555123",
            SenderRole = "visitor",
            Subject = "Bilgi",
            Message = "Detay alabilir miyim?",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-3),
            IsRead = false
        };

        harness.Db.VisitorMessages.Add(root);
        await harness.Db.SaveChangesAsync();

        var controller = harness.WithUser(
            new AccountControllerApi(
                harness.UserManager,
                harness.Configuration,
                harness.Db,
                new NoOpUserMessageAutomationService(),
                NullLogger<AccountControllerApi>.Instance),
            owner.Id);

        var action = await controller.ReplyMessage(new MobileReplyMessageRequest
        {
            MessageId = root.Id,
            Message = "Tabii, detaylar ilanda mevcut."
        });

        var ok = Assert.IsType<OkObjectResult>(action);
        Assert.NotNull(ok.Value);

        var allMessages = harness.Db.VisitorMessages
            .Where(x => x.ConversationId == "conv-reply")
            .OrderBy(x => x.CreatedAtUtc)
            .ToList();

        Assert.Equal(2, allMessages.Count);
        Assert.True(allMessages[0].IsRead);
        Assert.Equal("owner", allMessages[1].SenderRole);
        Assert.Equal(owner.Id, allMessages[1].SenderUserId);
    }
}

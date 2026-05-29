using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using SEN_T_PAZAR.Controllers;
using SEN_T_PAZAR.Services;
using Xunit;

namespace SenTPazar.Tests;

public class ApiMetaControllerTests
{
    [Fact]
    public void GetCategories_DoesNotExposeLandAsTopLevelCategory()
    {
        using var harness = new TestHarness();

        var controller = new ApiMetaController(new ListingCatalogService(), harness.Db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.ControllerContext.HttpContext.Request.Headers.AcceptLanguage = new StringValues("tr-TR");
        controller.ControllerContext.HttpContext.RequestServices = new ServiceCollection()
            .AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = controller.ControllerContext.HttpContext })
            .BuildServiceProvider();

        var action = controller.GetCategories();

        var ok = Assert.IsType<OkObjectResult>(action);
        var categories = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
        var payload = System.Text.Json.JsonSerializer.Serialize(categories);

        Assert.Contains("realestate", payload);
        Assert.DoesNotContain("\"code\":\"land\"", payload);
    }
}
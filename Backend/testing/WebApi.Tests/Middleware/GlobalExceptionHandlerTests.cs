﻿using System.Net;
using System.Net.Http.Json;
using Domain.Entities.OrderAggregate;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApi.Exceptions;
using WebApi.Features.Orders.Common;
using WebApi.Tests.TestCommon;

namespace WebApi.Tests.Middleware;

public class GlobalExceptionHandlerTests
: IntegrationTestsBase
{
    
    [Fact]
    public async Task WhenExceptionOccurs_ReturnsProblemDetails()
    {
        // ************ ARRANGE ************
        
        // Will force the CreateOrder use case to throw an exception
        var repository = new Mock<ISalesOrdersRepository>();
        repository.Setup(x => x.AddAsync(It.IsAny<SalesOrder>()))
            .ThrowsAsync(new EntityNotFoundException("The order does not exist."));
        

        var factory = CreateApplicationFactory(services =>
        {
            services.TestReplaceScopedService<ISalesOrdersRepository, SalesOrderRepository>(
                typeof(SalesOrderRepository), _ => repository.Object);
        });

        var client = factory.CreateClient();

        var endpoint = "api/sale-orders/" + Guid.NewGuid().ToString();
        
        // ************ ACT ************

        var response = await client.PostAsync(endpoint, null);
        
        // ************ ASSERT ************

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(result);
        Assert.Equal("The order does not exist.", result.Detail);
        Assert.Equal(404, result.Status);
    }
}
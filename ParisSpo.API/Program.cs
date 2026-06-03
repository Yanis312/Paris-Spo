using ParisSpo.Domain.Interfaces;
using ParisSpo.Infrastructure.Config;
using ParisSpo.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB"));

builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<IBetRepository, BetRepository>();
builder.Services.AddScoped<IBankrollRepository, BankrollRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();

builder.Services
    .AddGraphQLServer()
    .AddQueryType()
    .AddTypeExtension<ParisSpo.API.GraphQL.Queries.MatchQuery>()
    .AddTypeExtension<ParisSpo.API.GraphQL.Queries.BetQuery>()
    .AddTypeExtension<ParisSpo.API.GraphQL.Queries.BankrollQuery>()
    .AddMutationType()
    .AddTypeExtension<ParisSpo.API.GraphQL.Mutations.BetMutation>()
    .AddMongoDbFiltering()
    .AddMongoDbSorting()
    .AddMongoDbProjections()
    .AddMongoDbPagingProviders();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.MapGraphQL();

app.Run();

using ParisSpo.AI.Services;
using ParisSpo.Domain.Interfaces;
using ParisSpo.Infrastructure.Config;
using ParisSpo.Infrastructure.ExternalApis;
using ParisSpo.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// MongoDB
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDB"));

// External APIs config
builder.Services.Configure<FootballDataSettings>(builder.Configuration.GetSection("FootballData"));
builder.Services.Configure<TheOddsApiSettings>(builder.Configuration.GetSection("TheOddsApi"));
builder.Services.Configure<SportApi7Settings>(builder.Configuration.GetSection("SportApi7"));

// Repositories
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<IBetRepository, BetRepository>();
builder.Services.AddScoped<IBankrollRepository, BankrollRepository>();
builder.Services.AddScoped<ITeamRepository, TeamRepository>();

// External services — SportAPI7 (Sofascore) = source principale matchs + cotes réelles
builder.Services.AddScoped<SportApi7Service>();
builder.Services.AddScoped<IFootballDataService>(sp => sp.GetRequiredService<SportApi7Service>());
builder.Services.AddScoped<IOddsService>(sp => sp.GetRequiredService<SportApi7Service>());
builder.Services.AddScoped<MatchSyncService>();

// AI — OpenRouter avec fallback infini sur modèles gratuits
var openRouterKey = builder.Configuration["OpenRouter:ApiKey"] ?? "";
builder.Services.AddSingleton(new OpenRouterKernelFactory(openRouterKey));
builder.Services.AddScoped<FallbackKernelExecutor>();
builder.Services.AddScoped<IAiAnalysisService, MatchAnalysisAgent>();

// GraphQL
builder.Services
    .AddGraphQLServer()
    .AddQueryType()
    .AddTypeExtension<ParisSpo.API.GraphQL.Queries.MatchQuery>()
    .AddTypeExtension<ParisSpo.API.GraphQL.Queries.BetQuery>()
    .AddTypeExtension<ParisSpo.API.GraphQL.Queries.BankrollQuery>()
    .AddMutationType()
    .AddTypeExtension<ParisSpo.API.GraphQL.Mutations.BetMutation>()
    .AddTypeExtension<ParisSpo.API.GraphQL.Mutations.SyncMutation>()
    .AddTypeExtension<ParisSpo.API.GraphQL.Mutations.AiMutation>()
    .AddTypeExtension<ParisSpo.API.GraphQL.Mutations.TestMutation>()
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

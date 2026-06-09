using ParisSpo.Domain.Models;
using ParisSpo.Infrastructure.Services;

namespace ParisSpo.API.GraphQL.Queries;

[QueryType]
public class SimulationQuery
{
    public async Task<DailySimulation> DailySimulationAsync(
        double dailyBudget,
        [Service] SimulationService sim)
        => await sim.SimulateTodayAsync(dailyBudget);

    public async Task<DailySimulation> DailySimulationByDateAsync(
        double dailyBudget,
        DateTime date,
        [Service] SimulationService sim)
        => await sim.SimulateDayAsync(dailyBudget, date);
}

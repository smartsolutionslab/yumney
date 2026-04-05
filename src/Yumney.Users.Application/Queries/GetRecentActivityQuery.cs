using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Users.Application.Queries;

public sealed record GetRecentActivityQuery(int Limit = 5)
    : IQuery<Result<IReadOnlyList<UserActivityDto>>>;

using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Shared.Outcomes;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Users.Application.Queries;

public sealed record GetUserProfileQuery : IQuery<Result<UserProfileDto>>;

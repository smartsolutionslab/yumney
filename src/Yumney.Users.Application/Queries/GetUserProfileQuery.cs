using SmartSolutionsLab.Yumney.Shared.Common;
using SmartSolutionsLab.Yumney.Shared.CQRS;
using SmartSolutionsLab.Yumney.Users.Application.DTOs;

namespace SmartSolutionsLab.Yumney.Users.Application.Queries;

public sealed record GetUserProfileQuery : IQuery<Result<UserProfileDto>>;

using SmartSolutionsLab.Yumney.Shared.Abstractions;

namespace SmartSolutionsLab.Yumney.Recipes.Domain.Chat;

public sealed record ChatHistoryEntry(ChatRole Role, ChatMessageContent Content) : IValueObject;

namespace SmartSolutionsLab.Yumney.Shared.Events.Contracts;

/// <summary>
/// Published by the Users module when a user erases their account (US-101 / GDPR Art. 17).
/// Carries the Keycloak user identifier as a primitive so subscribers in every module
/// can wipe owner-scoped data without taking a Users.Domain dependency.
/// Handlers MUST be idempotent: this event may be re-delivered, and repeated handling
/// must not throw if the data is already gone.
/// </summary>
public sealed record UserAccountDeletedIntegrationEvent(string KeycloakUserId) : IntegrationEvent;

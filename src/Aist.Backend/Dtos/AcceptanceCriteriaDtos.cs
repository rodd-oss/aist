namespace Aist.Backend.Dtos;

public record CreateAcceptanceCriteriaRequest(Guid UserStoryId, string Description);
public record AcceptanceCriteriaResponse(Guid Id, Guid UserStoryId, string Description, bool IsMet);
public record UpdateAcceptanceCriteriaRequest(bool IsMet);

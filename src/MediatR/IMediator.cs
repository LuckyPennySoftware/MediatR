namespace Deluxe.MediatR;

/// <summary>
/// Defines a mediator to encapsulate request/response and publishing interaction patterns
/// </summary>
public interface IMediator : ISender, IPublisher
{
}
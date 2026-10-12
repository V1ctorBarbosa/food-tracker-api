using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using FoodTrack.Application.MealEstimates;
using Microsoft.Extensions.Options;

namespace FoodTrack.Infrastructure.MealEstimates;

/// <summary>Estimates a meal with Claude, asking for structured output so the answer always has the schema's shape.</summary>
public sealed class ClaudeMealEstimator : IMealEstimator
{
    // Leaves room for adaptive thinking before the JSON answer; a full plate needs only a few hundred tokens of output.
    private const int MaximumOutputTokens = 8_000;

    private readonly AnthropicClient _client;
    private readonly AnthropicOptions _options;

    public ClaudeMealEstimator(AnthropicClient client, IOptions<AnthropicOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<RecognizedMeal> EstimateAsync(MealEstimateInput input, CancellationToken cancellationToken = default)
    {
        var parameters = new MessageCreateParams
        {
            Model = _options.Model,
            MaxTokens = MaximumOutputTokens,
            System = MealEstimatePrompt.SystemPrompt,
            Messages = [new() { Role = Role.User, Content = BuildContent(input) }],
            OutputConfig = new OutputConfig
            {
                Format = new JsonOutputFormat { Schema = MealEstimatePrompt.BuildAnswerSchema() },
            },
        };

        var response = await SendAsync(parameters, cancellationToken);
        return RecognizedMealJsonParser.Parse(ReadAnswerText(response));
    }

    private static List<ContentBlockParam> BuildContent(MealEstimateInput input)
    {
        var content = new List<ContentBlockParam>();
        if (input.PhotoUrl is { } photoUrl)
        {
            content.Add(new ImageBlockParam { Source = new UrlImageSource { Url = photoUrl.ToString() } });
        }

        content.Add(new TextBlockParam { Text = MealEstimatePrompt.BuildDescriptionText(input.Description?.Value) });
        return content;
    }

    private async Task<Message> SendAsync(MessageCreateParams parameters, CancellationToken cancellationToken)
    {
        try
        {
            return await _client.Messages.Create(parameters, cancellationToken);
        }
        catch (AnthropicApiException exception)
        {
            throw new MealEstimatorUnavailableException("The estimator returned an error.", exception);
        }
        catch (AnthropicIOException exception)
        {
            throw new MealEstimatorUnavailableException("The estimator could not be reached.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MealEstimatorUnavailableException("The estimator took too long to answer.", exception);
        }
    }

    private static string ReadAnswerText(Message response)
    {
        if (response.StopReason != StopReason.EndTurn)
        {
            throw new MealEstimatorUnavailableException($"The estimator stopped before answering ({response.StopReason}).");
        }

        var answerText = string.Concat(response.Content.Select(block => block.Value).OfType<TextBlock>().Select(textBlock => textBlock.Text));
        if (string.IsNullOrWhiteSpace(answerText))
        {
            throw new MealEstimatorUnavailableException("The estimator gave an empty answer.");
        }

        return answerText;
    }
}

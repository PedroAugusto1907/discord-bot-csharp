using DiscordBot.Repository;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace DiscordBot.Commands;

public class GifCommand(GifRepository gifRepository) : ApplicationCommandModule<MessageCommandContext>
{
    [MessageCommand("Reagir com GIF")]
    public async Task GifAsync()
    {
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        var targetMessage = Context.Target;
        var gifUrl =  gifRepository.GetGif();

        await Context.Interaction.DeleteResponseAsync();

        await Context.Channel.SendMessageAsync(new MessageProperties()
            .WithContent(gifUrl)
            .WithMessageReference(MessageReferenceProperties.Reply(targetMessage.Id, failIfNotExists: false)));
    }
}
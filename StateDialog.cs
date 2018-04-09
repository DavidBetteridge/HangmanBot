namespace Microsoft.Bot.Sample.FormBot
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using System.Web;
    using global::FormBot;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;

    [Serializable]
    public class StateDialog : IDialog<object>
    {
        private string word;
        private bool[] letters;
        private int numberOfWrongAttempts;

        private string PickWordForUserToGuess()
        {
            var words = new string[] {  "Awkward",
                                        "Bagpipes",
                                        "Banjo",
                                        "Bungler",
                                        "Croquet",
                                        "Crypt",
                                        "Dwarves",
                                        "Fervid",
                                        "Fishhook",
                                        "Fjord",
                                        "Gazebo",
                                        "Gypsy",
                                        "Haiku",
                                        "Haphazard",
                                        "Hyphen",
                                        "Ivory",
                                        "Jazzy",
                                        "Jiffy",
                                        "Jinx",
                                        "Jukebox",
                                        "Kayak",
                                        "Kiosk",
                                        "Klutz",
                                        "Memento",
                                        "Mystify",
                                        "Numbskull",
                                        "Ostracize",
                                        "Oxygen",
                                        "Pajama",
                                        "Phlegm",
                                        "Pixel",
                                        "Polka",
                                        "Quad",
                                        "Quip",
                                        "Rhythmic",
                                        "Rogue",
                                        "Sphinx",
                                        "Squawk",
                                        "Swivel",
                                        "Toady",
                                        "Twelfth",
                                        "Unzip",
                                        "Waxy",
                                        "Wildebeest",
                                        "Yacht",
                                        "Zealous",
                                        "Zigzag",
                                        "Zippy",
                                        "Zombie" };
            var rnd = new Random();
            return words[rnd.Next(words.Length)];
        }

        private string WordToDisplay(string word)
        {
            var toDisplay = "";
            foreach (var letter in word.ToUpper())
            {
                if (letters[(char)letter - 'A'])
                    toDisplay += letter;
                else
                    toDisplay += "_____     ";
            }
            return toDisplay.Trim();
        }

        private void NewGame(IDialogContext context)
        {
            this.word = PickWordForUserToGuess().ToUpper();
            this.letters = new bool[26];
            this.numberOfWrongAttempts = 0;

            context.ConversationData.SetValue(ContextConstants.WordKey, word);
            context.ConversationData.SetValue(ContextConstants.LettersKey, letters);
        }

        private List<char> AvailableLetters()
        {
            var result = new List<char>();
            for (var c = 'A'; c <= 'Z'; c++)
            {
                if (!this.letters[c - 'A'])
                    result.Add(c);
            }
            return result;
        }

        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync($"Welcome to the Hangman bot. Would you like to play a game?");
            context.Wait(this.MessageReceivedAsync);
        }

        private async Task ChoiceReceivedAsync(IDialogContext context, IAwaitable<char> result)
        {
            var picked = await result;

            var w = "";
            if (context.ConversationData.TryGetValue<string>(ContextConstants.WordKey, out w))
            {
                this.word = w;
            }

            var l = default(bool[]);
            if (context.ConversationData.TryGetValue(ContextConstants.LettersKey, out l))
            {
                this.letters = l;
            }

            this.letters[(char)picked - 'A'] = true;

            if (this.word.IndexOf(picked) < 0)
            {
                numberOfWrongAttempts++;
                var replyMessage = context.MakeMessage();
                replyMessage.Text = "Sorry,  that's not correct";
                replyMessage.Attachments = new List<Attachment> { GetInlineAttachment(numberOfWrongAttempts) };
                await context.PostAsync(replyMessage);

                if (numberOfWrongAttempts == 11)
                {
                    await context.PostAsync($"Sorry you have lost. The word was {this.word}. Would you like to play again?");
                    context.Wait(this.MessageReceivedAsync);
                    return;
                }
            }

            context.ConversationData.SetValue(ContextConstants.LettersKey, letters);

            var display = WordToDisplay(this.word);
            await context.PostAsync(display);

            if (display == this.word.ToUpper())
            {
                await context.PostAsync($"Well done you have won. Would you like to play again?");
                context.Wait(this.MessageReceivedAsync);
            }
            else
            {
                AskForLetter(context);
            }
        }



        private static Attachment GetInlineAttachment(int numberOfWrongAttempts)
        {
            var imagePath = HttpContext.Current.Server.MapPath($"~/images/{numberOfWrongAttempts}.png");
            var imageData = Convert.ToBase64String(File.ReadAllBytes(imagePath));

            return new Attachment
            {
                Name = $"{numberOfWrongAttempts}-image.png",
                ContentType = "image/png",
                ContentUrl = $"data:image/png;base64,{imageData}"
            };

        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var message = await result;
            if (message.Text.ToLower() == "yes")
            {
                NewGame(context);

                var display = WordToDisplay(this.word);
                await context.PostAsync($"That's good,  right I've thought of a word. " + display);

                AskForLetter(context);
            }
            else if (message.Text.ToLower() == "no")
            {
                await context.PostAsync("Oh,  but I like playing games.  Would you like to play a game?");
            }
        }

        private void AskForLetter(IDialogContext context)
        {
            PromptDialog.Choice(
                        context: context,
                        resume: ChoiceReceivedAsync,
                        options: AvailableLetters(),
                        prompt: "Select a letter:",
                        retry: "Letter not available. Please try again.",
                        promptStyle: PromptStyle.Keyboard);
        }
    }
}
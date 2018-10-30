using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace JFKBot.App.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private enum Intents
        {
            Greetings,
            TopCryptonyms,
            KAPOKMeaning,
            WrittenClassifiedKAPOK,
            DidHeDoIt,
            Unknown
        }

        private const string OptionsKey = "$options?";

        private Dictionary<Intents, string> IntentToAnswerMap = new Dictionary<Intents, string>()
        {
            { Intents.Greetings, "Nice to be here. Thanks for having me. How can I help you?" },
            { Intents.TopCryptonyms, "KAPOK. RYBAT. GPAZURE." },
            { Intents.KAPOKMeaning, "KAPOK. It is the cable indicator for the highest level of document sensitivity. KAPOK is above RYBAT, which is above EYES ONLY." },
            { Intents.WrittenClassifiedKAPOK, "Yes, I have. Ask me a question." },
            { Intents.DidHeDoIt, "Here is a memo that I wrote..." },
            { Intents.Unknown, "..." },
        };

        private Dictionary<Intents, string> IntentToAudioUrlMap = new Dictionary<Intents, string>()
        {
            { Intents.Greetings, "https://jfkbotstore.blob.core.windows.net/jfkfilesbot-media/audio_greetings.wav" },
            { Intents.TopCryptonyms, "https://jfkbotstore.blob.core.windows.net/jfkfilesbot-media/audio_topcryptonyms.wav" },
            { Intents.KAPOKMeaning, "https://jfkbotstore.blob.core.windows.net/jfkfilesbot-media/audio_kapokmeaning.wav" },
            { Intents.WrittenClassifiedKAPOK, "https://jfkbotstore.blob.core.windows.net/jfkfilesbot-media/audio_writtenclassifiedkapok.wav" },
            { Intents.DidHeDoIt, "https://jfkbotstore.blob.core.windows.net/jfkfilesbot-media/audio_didhedoit.wav" },
        };

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            Activity activity = null;
            string message = string.Empty;

            try
            {
                activity = await result as Activity;
                message = WebUtility.HtmlDecode(activity.Text);

                // Extract options
                Dictionary<string, string> options = ExtractOptions(activity);

                bool useAudio = options.ContainsKey("audio") == true && options["audio"] == "true";

                // Extract intent
                Intents intent = Intents.Unknown;

                if (string.IsNullOrEmpty(message) == false)
                {
                    string messageToLower = message.ToLower().Trim();

                    if (messageToLower.Contains("welcome") == true)
                    {
                        intent = Intents.Greetings;
                    }
                    else if (messageToLower.Contains("cryptonym") == true && messageToLower.Contains("gp") == true)
                    {
                        intent = Intents.KAPOKMeaning;
                    }
                    else if (messageToLower.Contains("do") == true && messageToLower.Contains("it") == true)
                    {
                        intent = Intents.DidHeDoIt;
                    }
                }

                await PostAsync(context, intent);

                if (intent != Intents.Unknown && useAudio == true)
                {
                    await PostAudioAsync(context, IntentToAudioUrlMap[intent]);
                }

                if (intent == Intents.DidHeDoIt)
                {
                    await PostVideoAsync(context, "https://jfkbotstore.blob.core.windows.net/jfkfilesbot-media/hoover_answers_didhedoit.mp4");
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("MessageReceivedAsync throws exception: " + ex);

                if (activity != null)
                {
                    await PostAsync(context, Intents.Unknown);
                }
            }
            finally
            {
                // Wait for the next message
                context.Wait(MessageReceivedAsync);
            }
        }

        private Dictionary<string, string> ExtractOptions(Activity activity)
        {
            Dictionary<string, string> retOptions = new Dictionary<string, string>();

            string optionsValue = activity?.From?.Id;

            if (string.IsNullOrEmpty(optionsValue) == true || optionsValue.Contains(OptionsKey) == false)
            {
                return retOptions;
            }

            optionsValue = optionsValue.Replace(OptionsKey, "");

            retOptions = optionsValue
                          .Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                          .Select(s => s.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries))
                          .Where(part => part.Length == 2)
                          .ToDictionary(key => key[0].Trim(), value => value[1].Trim());

            return retOptions;
        }

        private async Task PostAsync(IDialogContext context, Intents intent)
        {
            string message = this.IntentToAnswerMap[intent];
            await context.PostAsync(text: message);
        }

        private async Task PostAudioAsync(IDialogContext context, string audioUrl)
        {
            // Create message
            IMessageActivity newMessage = context.MakeMessage();

            newMessage.Attachments = new List<Attachment>();

            newMessage.Attachments.Add(
                new AudioCard
                {
                    Autostart = true,
                    Media = new List<MediaUrl>
                    {
                        new MediaUrl()
                        {
                            Url = audioUrl
                        }
                    }
                }.ToAttachment()
            );

            // Post message
            await context.PostAsync(newMessage);
        }

        private async Task PostVideoAsync(IDialogContext context, string videoUrl)
        {
            // Create message
            IMessageActivity newMessage = context.MakeMessage();

            newMessage.Attachments = new List<Attachment>();

            newMessage.Attachments.Add(
                new VideoCard
                {
                    Media = new List<MediaUrl>
                    {
                        new MediaUrl()
                        {
                            Url = videoUrl
                        }
                    }
                }.ToAttachment()
            );
    
            // Post message
            await context.PostAsync(newMessage);
        }
    }
}
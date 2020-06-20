using ImageMagick;
using Imgur.API;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Models;
using NReco.VideoConverter;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using YoutubeExplode;
using YoutubeExplode.Models.MediaStreams;
using File = System.IO.File;

namespace Think_Post_Generator
{
    class Program
    {
        static YoutubeClient clientYT;
        static YoutubeExplode.Models.Video video;
        static TimeSpan duration;
        static ITelegramBotClient bot;
        static int step = -1;
        static string link = string.Empty, output, picLink;
        static string title, author, genre;
        static int titleFontSize = 90, titleLen, authorFontSize = 140, authorLen;
        static string primColor, secColor, backColor;
        static ReplyKeyboardMarkup replyKeyboardMarkupColor = new[]
                                {
                                new[] { "333333", "FAFAFA" },
                                new[] { "FFB900", "E74856", "0078D7", "0099BC", "7A7574", "767676" },
                                new[] { "FF8C00", "E81123", "0063B1", "2D7D9A", "5D5A58", "4C4A48" },
                                new[] { "F7630C", "EA005E", "8E8CD8", "00B7C3", "68768A", "69797E" },
                                new[] { "CA5010", "C30052", "6B69D6", "038387", "515C6B", "4A5459" },
                                new[] { "DA3B01", "E3008C", "8764B8", "00B294", "567C73", "647C64" },
                                new[] { "EF6950", "BF0077", "744DA9", "018574", "486860", "525E54" },
                                new[] { "D13438", "C239B3", "B146C2", "00CC6A", "498205", "847545" },
                                new[] { "FF4343", "9A0089", "881798", "10893E", "107C10", "7E735F" }
                                };

        static void Main()
        {

            clientYT = new YoutubeClient();
            bot = new TelegramBotClient("");
            User me = bot.GetMeAsync().Result;
            Console.WriteLine($"{me.FirstName} is running");
            bot.OnMessage += Bot_OnMessage;
            bot.StartReceiving();
            Thread.Sleep(int.MaxValue);
        }

        static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message != null)
            {
                Telegram.Bot.Types.Message message = e.Message;
                Console.WriteLine($"Received a message from {message.Chat.FirstName} at {message.Date}. It says: \"{e.Message.Text}\"");

                switch (message.Text)
                {
                    case "/stop":
                        step = -1;
                        break;
                    case "/start":

                        await bot.SendTextMessageAsync(
                            chatId: message.Chat,
                            text: "Send link",
                            replyToMessageId: message.MessageId
                            );

                        step = 0;
                        break;
                    default:
                        if (step == 0)
                        {
                            link = message.Text;

                            await bot.SendTextMessageAsync(
                                 chatId: message.Chat,
                                 text: "Send title",
                                 replyToMessageId: message.MessageId
                                 );

                            step = 1;

                            try
                            {

                                string id = YoutubeClient.ParseVideoId(link);

                                MediaStreamInfoSet streamInfoSet = await clientYT.GetVideoMediaStreamInfosAsync(id); //So54Khf7bB8
                                video = await clientYT.GetVideoAsync(id);
                                duration = video.Duration; // 00:07:14
                                AudioStreamInfo streamInfo = streamInfoSet.Audio.OrderBy(s => s.Bitrate).First();
                                string ext = streamInfo.Container.GetFileExtension();

                                Console.WriteLine("Downloading audio");

                                await clientYT.DownloadMediaStreamAsync(streamInfo, $"Audio\\audio.{ext}");

                                Console.WriteLine("Audio has been downloaded. Converting audio");

                                FFMpegConverter convertAudio = new NReco.VideoConverter.FFMpegConverter();
                                convertAudio.ConvertMedia($"Audio\\audio.{ext}", null, "Audio\\audio.ogg", null, new ConvertSettings()
                                {
                                    //CustomOutputArgs = $"-b:a {streamInfo.Bitrate}"
                                    CustomOutputArgs = $"-c:a libopus -b:a {streamInfo.Bitrate}"
                                });

                                Console.WriteLine("Converting has been completed.");

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                                await bot.SendTextMessageAsync(
                                     chatId: message.Chat,
                                     text: "The link that you have sent is not valid or the file size is too big. Please, send it again",
                                     replyToMessageId: message.MessageId
                                     );
                                step = 0;
                            }
                        }
                        else if (step == 1)
                        {
                            title = message.Text.ToUpper();
                            titleLen = title.Length - 27;

                            output = $"<b>{message.Text}</b>\n";

                            await bot.SendTextMessageAsync(
                                 chatId: message.Chat,
                                 text: "Send author",
                                 replyToMessageId: message.MessageId
                                 );

                            step = 2;
                        }
                        else if (step == 2)
                        {
                            author = message.Text.ToUpper();
                            authorLen = author.Length - 14;

                            output += $"{message.Text}\n\n";

                            await bot.SendTextMessageAsync(
                                 chatId: message.Chat,
                                 text: "Send desription",
                                 replyToMessageId: message.MessageId
                                 );

                            step = 3;
                        }
                        else if (step == 3)
                        {
                            output += $"{message.Text}\n\n";

                            ReplyKeyboardMarkup replyKeyboardMarkup = new[]
                            {
                                new[] { "Бизнес", "Биография", "Психология", "Саморазвитие" },
                                new[] { "Философия", "Наука", "История", "Финансы" }
                            };

                            replyKeyboardMarkup.OneTimeKeyboard = true;

                            await bot.SendTextMessageAsync(
                                 chatId: message.Chat,
                                 text: "Send genre",
                                 replyToMessageId: message.MessageId,
                                 replyMarkup: replyKeyboardMarkup
                                 );

                            output += $"🔈 <b>Продолжительность ≈</b> {Math.Round(duration.TotalMinutes, MidpointRounding.AwayFromZero)} минут\n";

                            step = 4;
                        }
                        else if (step == 4)
                        {
                            genre = message.Text.ToUpper();
                            output += $"📝 <b>Жанр:</b> #{message.Text}";

                            await bot.SendTextMessageAsync(
                                 chatId: message.Chat,
                                 text: "Send image",
                                 replyToMessageId: message.MessageId
                                 );

                            step = 5;
                        }
                        else if (step == 5)
                        {
                            if (message.Type == MessageType.Photo)
                            {
                                FileStream fs = System.IO.File.OpenWrite("Image\\cover.png");
                                await bot.GetInfoAndDownloadFileAsync(message.Photo.Last().FileId, fs);
                                fs.Close();

                                await bot.SendTextMessageAsync(
                                    chatId: message.Chat,
                                    text: "Cover image received",
                                    replyToMessageId: message.MessageId
                                      );

                                FileStream pltrFile = File.OpenRead("Image\\palitre.png");

                                await bot.SendPhotoAsync(
                                    chatId: message.Chat,
                                    photo: pltrFile,
                                    caption: "Choose your colors. First, send me primary color",
                                    replyMarkup: replyKeyboardMarkupColor
                                    );

                                pltrFile.Close();
                                step = 6;
                            }
                            else
                            {
                                await bot.SendTextMessageAsync(message.Chat, "I implores you to send photo and not anything else, capish?");
                                step = 5;
                            }
                        }
                        else if (step == 6)
                        {

                            if (message.Text[0] == '#')
                            {
                                primColor = message.Text;
                            }
                            else
                            {
                                primColor = '#' + message.Text;
                            }
                            await bot.SendTextMessageAsync(
                                chatId: message.Chat,
                                text: "I have got primary color. Now, send me secondary color"
                                );

                            step = 7;
                        }
                        else if (step == 7)
                        {
                            if (message.Text[0] == '#')
                            {
                                secColor = message.Text;
                            }
                            else
                            {
                                secColor = '#' + message.Text;
                            }

                            replyKeyboardMarkupColor.OneTimeKeyboard = true;
                            await bot.SendTextMessageAsync(
                                chatId: message.Chat,
                                text: "I have got secondary color. Now, send me background color",
                                replyMarkup: replyKeyboardMarkupColor
                                );

                            step = 8;
                        }
                        else if (step == 8)
                        {
                            if (message.Text[0] == '#')
                            {
                                backColor = message.Text;
                            }
                            else
                            {
                                backColor = '#' + message.Text;
                            }



                            await bot.SendTextMessageAsync(
                                chatId: message.Chat,
                                text: "I have got background color. Now, I will send you a final post"
                                );

                            if (authorLen > 0)
                            {
                                authorFontSize -= authorLen * 8;
                            }
                            if (titleLen > 0)
                            {
                                titleFontSize -= titleLen * 5;
                            }

                            using (MagickImage form = new MagickImage(MagickColors.White, 1920, 1080))
                            {
                                new Drawables()

                                //Draw rectangle
                                .FillColor(new MagickColor(backColor))
                                .Rectangle(0, 0, 840, 1080)

                               //Draw hashtag
                               .FontPointSize(100)
                               .Font("Font\\nrwstr.otf")
                               .FillColor(new MagickColor(secColor))
                               .Text(30, 124, "#")

                               //Draw genre
                               .Font("Font\\amcap.ttf")
                               .Text(124, 124, genre)

                               //Draw title
                               .FontPointSize(titleFontSize)
                               .FillColor(new MagickColor(primColor))
                               .Text(30, 520, title)

                               //Draw name of the author
                               .FontPointSize(authorFontSize)
                               .FillColor(new MagickColor(secColor))
                               .Text(30, 650, author)

                               //Draw name of the channel
                               .FillColor(new MagickColor(primColor))
                               .FontPointSize(50)
                               .Text(150, 960, "THINK! - АУДИОПОДКАСТЫ")

                               //Draw link of the channel
                               .Font("Font\\nrwstr.otf")
                               .FillColor(new MagickColor(secColor))
                               .FontPointSize(40)
                               .Text(150, 1010, "T.ME/THINKAUDIO")

                               .Draw(form);

                                using MagickImage cover = new MagickImage("Image\\cover.png");
                                cover.Resize(1280, 1280);
                                form.Composite(cover, 840, 0, CompositeOperator.Over);

                                //Draw logo of the channel
                                using MagickImage logo = new MagickImage("Image\\logo.png");
                                logo.Alpha(AlphaOption.Set);
                                logo.ColorFuzz = new Percentage(0);
                                logo.Settings.BackgroundColor = MagickColors.Transparent;
                                //logo.Settings.FillColor = MagickColors.White;
                                logo.Opaque(MagickColors.White, new MagickColor(primColor));
                                form.Composite(logo, 30, 920, CompositeOperator.Over);

                                form.Write("Image\\template.png");
                            }

                           await bot.SendTextMessageAsync(message.Chat, "Template has been created. Sending it to you");

                            using (var stream = new FileStream("Image\\template.png", FileMode.Open))
                            {
                                InputOnlineFile inputOnlineFile = new InputOnlineFile(stream, "template.png");
                                await bot.SendDocumentAsync(message.Chat, inputOnlineFile);
                            }
                           

                            try
                            {
                                ImgurClient client = new ImgurClient("", "");
                                ImageEndpoint endpoint = new ImageEndpoint(client);
                                IImage image;
                                using (FileStream fs = new FileStream("Image\\template.png", FileMode.Open))
                                {
                                    await bot.SendTextMessageAsync(message.Chat, "Uploading Image to Imgur");
                                    image = await endpoint.UploadImageStreamAsync(fs);

                                }
                                picLink = image.Link;
                                Console.WriteLine("Image uploaded. Image Url: " + image.Link);

                                output += $"<a href=\"{picLink}\">&#8205;</a>";

                                await bot.SendTextMessageAsync(
                                    chatId: message.Chat,
                                    text: output,
                                    parseMode: ParseMode.Html
                                    );
                            }
                            catch (ImgurException imgurEx)
                            {
                                Debug.Write("An error occurred uploading an image to Imgur.");
                                Debug.Write(imgurEx.Message);
                            }

                            using (var stream = new FileStream("Audio\\audio.ogg", FileMode.Open))
                            {
                                 await bot.SendVoiceAsync(
                                       chatId: message.Chat,
                                       voice: stream
                                       
                                       );
                            }
                            step = -1;

                        }
                        break;
                }

            }
        }

    }
}

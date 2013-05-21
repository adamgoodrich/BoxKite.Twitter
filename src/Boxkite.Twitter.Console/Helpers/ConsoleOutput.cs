﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoxKite.Twitter.Models;

namespace BoxKite.Twitter.Console.Helpers
{
    public static class ConsoleOutput
    {
        public static void PrintError(String err, ConsoleColor fgColour = ConsoleColor.Red,
            ConsoleColor bgColour = ConsoleColor.Black)
        {
            PrintMessage(err, ConsoleColor.Red);
        }

        public static void PrintTweet(Tweet t, ConsoleColor fgColour = ConsoleColor.Gray,
            ConsoleColor bgColour = ConsoleColor.Black)
        {
            System.Console.BackgroundColor = bgColour;
            if (!t.twitterFaulted)
            {
                PrintLineHeader(t.User.ScreenName, ConsoleColor.Gray);
                System.Console.ForegroundColor = fgColour;
                System.Console.WriteLine("{0}", t.Text);
            }
            else
                PrintError(t.twitterControlMessage.twitter_error_message, fgColour, bgColour);

            System.Console.ResetColor();
        }

        public static void PrintDirect(DirectMessage d, ConsoleColor fgColour = ConsoleColor.Magenta,
            ConsoleColor bgColour = ConsoleColor.Blue)
        {
            System.Console.BackgroundColor = bgColour;
            if (!d.twitterFaulted)
            {
                PrintLineHeader(d.SenderScreenName, ConsoleColor.Gray);
                System.Console.ForegroundColor = fgColour;
                System.Console.WriteLine("{0}", d.Text);
            }
            else
                PrintError(d.twitterControlMessage.twitter_error_message, fgColour, bgColour);
            System.Console.ResetColor();
        }

        public static void PrintEvent(Event e, ConsoleColor fgColour, ConsoleColor bgColour = ConsoleColor.Black)
        {
            System.Console.BackgroundColor = bgColour;
            PrintLineHeader(e.EventName, ConsoleColor.Gray);
            System.Console.ForegroundColor = fgColour;
            var sourceTweet = ((TweetEvent) e).TargetObject;
            if (sourceTweet != null)
                PrintTweet(sourceTweet, fgColour, bgColour);
            System.Console.ResetColor();
        }

        public static void PrintLineHeader(string text, ConsoleColor fgColour = ConsoleColor.Green)
        {
            System.Console.ForegroundColor = fgColour;
            System.Console.Write("{0}: ", text);
            System.Console.ResetColor();
        }

        public static void PrintMessage(string text, ConsoleColor fgColour = ConsoleColor.Green)
        {
            System.Console.ForegroundColor = fgColour;
            System.Console.WriteLine("{0}", text);
            System.Console.ResetColor();
        }

    }
}


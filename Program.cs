using System.Text.RegularExpressions;
using System.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

namespace TheExclusiveBot
{
    /*
    class user
        userpoints
        userposition 
    */
    class Program
    {
        //All console write line are for debug none are necesary
        static void Main(string[] args)
        {
            IrcClient client = new IrcClient("irc.twitch.tv", 6667, "theexclusivebot", "oauth:t7wymmwj75zgl2ppnp512stu65ubi2", "theexclusivefurry");

            var pinger = new Pinger(client);
            pinger.Start();

            //Takes the count from the text file and cinverts it nu int
            List<string> countFileList = File.ReadAllLines("Count.txt").ToList();
            int cuteCounterTotal = Convert.ToInt32(countFileList[0]);
            int cuteCounterCurrent = 0;

            //While lopp that the code runs in.
            while (true)
            {
                // Sort the current cute list so that it order by most points first
                //Make a list with the int and string of each user and not take the first wich is the overall points  
                List<(int, string)> isueg = new List<(int, string)> { };
                foreach (var item in countFileList)
                {
                    if (item != countFileList[0])
                    {
                        string[] tempArray = item.Split(' ');
                        isueg.Add(((int.Parse(tempArray[0])), (tempArray[1])));
                    }
                }
                //Sort it based on the int using a Linq sorthing method and reverse the list so its in heighest first.
                isueg.Sort((thing1, thing2) => thing1.Item1.CompareTo(thing2.Item1));
                isueg.Reverse();
                //Adds the sorted list back to the main list 
                int tempCounter = 0;
                foreach (var item in isueg)
                {
                    tempCounter++;
                    countFileList[tempCounter] = item.Item1.ToString() + " " + item.Item2;
                }
                //Create the user varibles
                string username = " ";
                int userPoints = 0;
                int userPosition = 0;

                //Get and write the message to console
                Console.WriteLine("Reading message");
                var message = client.ReadMessage();
                Console.WriteLine($"Message: {message}");

                //Search the message for the word cute and adding points to current and total and the users.
                if (WordSearch(message, ref cuteCounterTotal, ref cuteCounterCurrent, ref username, ref userPoints, ref userPosition, ref countFileList) == true)
                {
                    //If it found the word cute it send a message in chat to say how many cutes this stream and it updates the list with the new numbers.
                    string cuteCounterCurrentString = Convert.ToString(cuteCounterCurrent);
                    client.SendChatMessage("Cutes written this stream: " + cuteCounterCurrentString);
                    Console.WriteLine($"{username} {userPoints} {userPosition}");
                    countFileList[userPosition] = userPoints.ToString() + " " + username;
                }
                Console.WriteLine(cuteCounterTotal);

                //Update the save file with the new information.
                countFileList[0] = (Convert.ToString(cuteCounterTotal));
                File.WriteAllLines(@"Count.txt", countFileList);

                //Check if the msesage starts with a ! then check if its a viable command
                string maybeCommand;
                if (checkCommand(message, out maybeCommand) == true)
                {
                    if (maybeCommand != null)
                    {
                        //If the command is helpcute then explain what the bot does and say the command to se other commands.
                        if (maybeCommand.ToLower() == "helpcute")
                        {
                            client.SendChatMessage("@" + username + " This bot counts the number of cutes written in this chat. It count both this stream and over all stream since the bot got activated. Type !cuteCommands to see see possible commands");
                        }
                        //If the command is mycutes the it will tell the user ther cute count and waht position on the scoreboard they are.
                        if (maybeCommand.ToLower() == "mycutes")
                        {
                            //Check the list for the username. If it exist take the usernames points else make it
                            int i = 0;
                            bool userExist = false;
                            bool userGotPoints = false;
                            foreach (var item in countFileList)
                            {
                                if (item != countFileList[0] && item.Split(" ")[1] == username)
                                {
                                    userPosition = i;
                                    if (userGotPoints == false)
                                    {
                                        userPoints = Convert.ToInt32(item.Split(" ")[0]);
                                        userGotPoints = true;
                                        Console.WriteLine("hello");
                                    }
                                    userExist = true;
                                    Console.WriteLine("yes");

                                    client.SendChatMessage("@" + username + " Your cute count is " + userPoints + " and you are number " + i + " on the scoreboard.");
                                }
                                i++;
                            }
                            if (userExist == false)
                            {
                                client.SendChatMessage("@" + username + " you have not said cute in this chat how weird. You should start.");
                            }
                        }
                        //If the command is cutecommands then tell the user all the possible commands and what they do.
                        if (maybeCommand.ToLower() == "cutecommands")
                        {
                            client.SendChatMessage("@" + username + "  Type !helpCute to see what this bot does. !MyCutes to see the number of cutes you have written in total and your position on the scoreboard. !CuteTop5 to see the top 5 people with the most cutes written");
                        }
                        //Tell the user the current top 5 on the cute scoreboard
                        if (maybeCommand.ToLower() == "cutetop5")
                        {
                            //New array for the current top 5 and takes them from the File list.
                            string[] top5Array = new string[5];
                            for (var i = 0; i < 5; i++)
                            {
                                top5Array[i] = countFileList[i + 1];
                            }
                            client.SendChatMessage("@" + username + " The current top 5 for the cute count is 1 with " + top5Array[0] + ", 2 with " + top5Array[1] + ", 3 with " + top5Array[2] + ", 4 with " + top5Array[3] + ", 5 with " + top5Array[4]);
                        }

                    }
                }
            }
        }
        /// <summary>
        /// Checks a message if it contains one of the search words and if it does adds to there points and to the total and if they dont have any earlier points then it adds them to thi list.  And returns False or True if it contains or not,
        /// </summary>
        /// <param name="message">The message from the Twitch Chat</param>
        /// <param name="checkWordCountTotal"></param>
        /// <param name="checkWordCountCurrent"></param>
        /// <param name="username">Username of the person from the Twitch chat</param>
        /// <param name="userPoints">The currents points of the user from the messeage</param>
        /// <param name="userPosition">The current user position on the save list</param>
        /// <param name="countFileList">The save file List</param>
        /// <returns></returns>
        static bool WordSearch(string message, ref int checkWordCountTotal, ref int checkWordCountCurrent, ref string username, ref int userPoints, ref int userPosition, ref List<string> countFileList)
        {
            bool cuteYes = false;
            //Split the message inte to seprate words
            string[] words = message.Split(" ");
            //Seperate the username
            username = (words[0].Split("!")[0]).TrimStart(':');

            //Loop to check each word if the match the check word
            bool userExist = false;
            bool userGotPoints = false;
            foreach (var word in words)
            {
                Match m = Regex.Match(word, @"^(.*?(cute|cutie|qt))+.*?");
                
               // m.Groups[1].Value;
                //m.Groups[1].Captures.Count;
                //Use regex to check the word for cute or cutie
                //Regex checkCute = new Regex("cute");
                //Regex checkCutie = new Regex("cutie");
                //Array with the commands so they dont count on the cute meter.
                string[] commands = { ":!mycutes", ":!helpcute", ":!cutecommands", ":!cutetop5" };
                //(checkCute.IsMatch(word.ToLower()) || checkCutie.IsMatch(word.ToLower()))
                if ( m.Success && commands.Contains(word.ToLower()) == false)
                {
                    //Check the list for the username. If it exist take the usernames points else make it
                    int i = 0;
                    foreach (var item in countFileList)
                    {
                        if (item != countFileList[0] && item.Split(" ")[1] == username)
                        {
                            userPosition = i;
                            if (userGotPoints == false)
                            {
                                userPoints = Convert.ToInt32(item.Split(" ")[0]);
                                userGotPoints = true;
                                Console.WriteLine("hello");
                            }
                            userExist = true;
                            Console.WriteLine("yes");
                        }
                        i++;
                    }
                    if (userExist == false)
                    {
                        countFileList.Add("1 " + username);
                        Console.WriteLine("no");
                    }
                    Console.WriteLine(word);
                    checkWordCountTotal += m.Groups[1].Captures.Count;
                    checkWordCountCurrent += m.Groups[1].Captures.Count;
                    userPoints++;
                    cuteYes = true;
                }
            }
            if (cuteYes == true)
            {
                return (true);
            }
            else
            {
                return (false);
            }
        }
        /// <summary>
        /// Checks if the first charakter is ! of the message and returns False or True.
        /// </summary>
        /// <param name="message">The message from the Twitch Chat</param>
        /// <param name="maybeCommand"></param>
        /// <returns></returns>
        static bool checkCommand(string message, out string? maybeCommand)
        {
            //Split the message at : and check if it became more than 2 segments otherwise stop the function
            string[] splitMessage = message.Split(":");
            if (splitMessage.Length > 2)
            {
                //take the first charakter of the message that the user wrote. and output true if it was !
                char firstChar = splitMessage[2][0];
                Console.WriteLine(firstChar);
                if (firstChar == '!')
                {
                    Console.WriteLine("GEG");
                    maybeCommand = splitMessage[2].TrimStart('!');
                    return true;
                }
                else
                {
                    Console.WriteLine("brög");
                    maybeCommand = null;
                    return false;
                }
            }
            maybeCommand = null;
            return false;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Drawing;
using LeagueBot.Patterns;
using LeagueBot.ApiHelpers;
using LeagueBot.Game.Enums;

namespace LeagueBot
{
    public class Start : PatternScript
    {
        private static ChampionEnum[] Champions = new ChampionEnum[]
        {
            ChampionEnum.Annie,
        };
		
		private static ChampionEnum[] Bans = new ChampionEnum[]
        {
            ChampionEnum.Veigar,
        };

        private static QueueEnum QueueType = QueueEnum.Ranked;

        
        public override void Execute()
        {
            client.openClient();

            bot.log("Waiting for league client process...");

            bot.waitProcessOpen(Constants.ClientProcessName);

            bot.bringProcessToFront(Constants.ClientProcessName);
            
            bot.centerProcess(Constants.ClientProcessName);   

            client.initialize(); // read current league process informations

            bot.log("Waiting for league client to be ready...");

            client.waitClientReady(); 

            while (!client.loadSummoner())
            {
                bot.warn("Unable to load summoner. Retrying in 10 seconds.");
                bot.wait(1000);
            }

            bot.log("Summoner loaded "+ client.summoner.displayName);

            bot.wait(3000);

            client.createLobby(QueueType);
			bot.wait(3000);
            client.pickRoles();

			client.Invite();
			bot.wait(60000);

            ProcessMatch();
        

        }
        private void ProcessMatch()
        {
            bot.log("Searching match...");
            
            bot.wait(3000);

            SearchMatchResult result = client.searchMatch();

            while (result != SearchMatchResult.Ok)
            {   
                switch (result)
                {
                    case SearchMatchResult.GatekeeperRestricted:
                       bot.warn("Cannot search match. Queue dodge timer. Retrying in 20 seconds.");
                       bot.wait(1000 * 20);
                       break;
                    case SearchMatchResult.QueueNotEnabled:
                       client.createLobby(QueueType);
                       bot.warn("Cannot search match. Creating lobby...");
                       bot.wait(1000 * 10);
                       break;
                    case SearchMatchResult.InvalidLobby:
                       bot.warn("Cannot search match. Client not ready. Retrying in 10 seconds.");
                        bot.wait(1000 * 10);
                       break;
                }

               

                result = client.searchMatch();
            }

            bool isMatchFound = false;

            while (!isMatchFound)
            {
                isMatchFound = client.isMatchFound();
                bot.wait(1000);

            }

            while (client.getGameflowPhase() != GameflowPhaseEnum.ChampSelect)
            {
                client.acceptMatch();
                bot.wait(1000);
            }


            bot.log("Match founded.");

            bot.wait(30000);

            bool banned = false;
			
			bool picked = false;

            int championIndex = 0;

            while (!banned)
            {
                ChampionPickResult pickResult = client.pickChampion(Bans[championIndex]);

                switch (pickResult)
                {
                    case ChampionPickResult.Ok:
                        bot.log(Bans[championIndex] + " banned successfully");
                        banned = true;
                        break;
                    case ChampionPickResult.ChampionNotOwned:
                        bot.warn("Error the request champion is not owned.");
                        break;
                    case ChampionPickResult.ChampionPicked:
                        bot.warn("Someone already pick your champion");
                        break;
                    default:
                        break;
                }
                bot.wait(1000);
            }
			
			while (!picked)
            {
                ChampionPickResult pickResult = client.pickChampion(Champions[championIndex]);

                switch (pickResult)
                {
                    case ChampionPickResult.Ok:
                        bot.log(Champions[championIndex] + " picked successfully");
                        picked = true;
                        break;
                    case ChampionPickResult.ChampionNotOwned:
                        bot.warn("Error the request champion is not owned.");
                        break;
                    case ChampionPickResult.ChampionPicked:
                        bot.warn("Someone already pick your champion");
                        break;
                    default:
                        break;
                }

                bot.wait(1000);
            }

            bot.log("Waiting....");

            GameflowPhaseEnum currentPhase = client.getGameflowPhase();

            while (currentPhase != GameflowPhaseEnum.InProgress)
            {
                if (currentPhase != GameflowPhaseEnum.ChampSelect)
                {
                    bot.log("Game was dodged, finding match....");
                    ProcessMatch();
                    return;
                }
                bot.wait(1000);
                currentPhase = client.getGameflowPhase();
				bot.log(client.getGameflowPhase());
            }

            bot.executePattern("GameOn");
        }
    }
}
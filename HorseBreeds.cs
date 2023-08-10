﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using Rust.Modular;
using UnityEngine;

///To Do
/// include costs per breed

namespace Oxide.Plugins
{
    [Info("Horse Breeds", "iLakSkiL", "1.0.0")]
    [Description("Change the breed of your horse.")]
    public class HorseBreeds : RustPlugin
    {
        [PluginReference]
        private Plugin ServerRewards, Economics;

        #region Configuration

        private Configuration _config;
        public class Configuration
        {
            [JsonProperty(PropertyName = "Use ServerRewards")]
            public bool currencySR = false;

            [JsonProperty(PropertyName = "Use Economics")]
            public bool currencyEC = false;

            [JsonProperty(PropertyName = "Horse Breed Costs")]
            public Costs costs = new Costs();

            public class Costs
            {
                [JsonProperty(PropertyName = "Dapple Grey")]
                public double cost4 = 20;

                [JsonProperty(PropertyName = "Red Roan")]
                public double cost7 = 20;

                [JsonProperty(PropertyName = "Appalosa")]
                public double cost0 = 30;

                [JsonProperty(PropertyName = "Bay")]
                public double cost1 = 30;

                [JsonProperty(PropertyName = "Buckskin")]
                public double cost2 = 30;

                [JsonProperty(PropertyName = "Pinto")]
                public double cost6 = 30;

                [JsonProperty(PropertyName = "Chestnut")]
                public double cost3 = 40;

                [JsonProperty(PropertyName = "Piebald")]
                public double cost5 = 40;

                [JsonProperty(PropertyName = "White Thoroughbred")]
                public double cost8 = 50;

                [JsonProperty(PropertyName = "Black Thoroughbred")]
                public double cost9 = 50;
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                SaveConfig();
            }
            catch
            {
                PrintError("Error reading config, please check!");
            }
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
            Puts("Loading Default Config");
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion

        #region Hooks

        void Loaded()
        {
            permission.RegisterPermission("horsebreeds.use", this);
            permission.RegisterPermission("horsebreeds.bypass", this);
            //check for using both currencies
            if (_config.currencySR && _config.currencyEC)
            {
                Puts("ERROR: Cannot use both ServerRewards and Economics. Neither currency will be used.");
                LoadDefaultConfig();
            }
        }

        #endregion

        #region Commands

        [ChatCommand("horse")]
        private void horseChatCmd(BasePlayer player, string command, string[] args)
        {
            //null check
            if (player == null || command == null || args == null) return;

            //check for player permission
            if (!(permission.UserHasPermission(player.UserIDString, "horsebreeds.use")))
            {
                Player.Message(player, "You do not have permission to use this command.");
                return;
            }

            //Mounted on Horse check
            if (player.GetMountedVehicle() == null || !player.GetMountedVehicle().ToString().Contains("ridablehorse"))
            {
                Player.Message(player, "You must be on a horse to use this command.");
                return;
            }

            //breed variable check
            int breed;
            string breedName;
            double cost;
            if (!IsValidBreed(args[0], out breed, out breedName, out cost))
            {
                Puts("Error: Invalid breed type");
                return;
            }

            if (permission.UserHasPermission(player.UserIDString, "horsebreeds.bypass")) cost = 0;
            ChangeHorse(player, breed, breedName, cost);
        }

        [ConsoleCommand("horse")]
        private void horseConsoleCmd(ConsoleSystem.Arg arg)
        {
            //only admin and console can use
            if (!arg.IsAdmin) return;

            if (arg.Args == null || arg.Args[1].Length < 1) return;

            //player checks
            var target = RustCore.FindPlayer(arg.Args[0]);
            if (target == null)
            {
                Puts($"Player '{arg.Args[0]}' not found");
                return;
            }
            if (!target.IsConnected)
            {
                Puts($"Player '{arg.Args[0]}' is not online");
                return;
            }
            if (target.GetMountedVehicle() == null || !target.GetMountedVehicle().ToString().Contains("ridablehorse"))
            {
                Puts($"Player '{arg.Args[0]}' is not on a horse");
                return;
            }

            //breed variable check
            int breed;
            string breedName;
            double cost;
            if (!IsValidBreed(arg.Args[1], out breed, out breedName, out cost))
            {
                Puts("Error: Invalid breed type");
                return;
            }

            if (permission.UserHasPermission(target.UserIDString, "horsebreeds.bypass")) cost = 0;
            ChangeHorse(target, breed, breedName, cost);


        }

        #endregion

        #region Helpers

        private void ChangeHorse(BasePlayer player, int breed, string breedName, double cost)
        {
            // code to run for ServerRewards
            if (_config.currencySR)
            {
                int balance = (int)ServerRewards?.Call("CheckPoints", player.userID);

                //balance check
                if (balance != null && balance < cost)
                {
                    Player.Message(player, "Horse Breeds: Insufficient balance");
                    return;
                }

                var horse = player.GetMountedVehicle() as RidableHorse;

                //apply horse breed and charge ServerRewards
                horse.ApplyBreed(breed);
                ServerRewards?.Call("TakePoints", player.userID, (int)Math.Round(cost));
                Player.Message(player, $"Your horse breed is now {breedName}");
                Puts($"{player.displayName}'s horse breed has been changed to {breedName}.");
                return;
            }

            //code to run for Economics
            if (_config.currencyEC)
            {
                double balance = (double)Economics?.Call("Balance", player.userID);

                //balance check
                if (balance != null && balance < cost)
                {
                    Player.Message(player, "Horse Breeds: Insufficient balance");
                    return;
                }

                var horse = player.GetMountedVehicle() as RidableHorse;

                //apply horse breed and charge Economics
                horse.ApplyBreed(breed);
                Economics?.Call("Withdraw", player.userID, cost);
                Player.Message(player, $"Your horse breed is now {breedName}");
                Puts($"{player.displayName}'s horse breed has been changed to {breedName}.");
                return;
            }

            //code to run when not using either currency
            else
            {
                var horse = player.GetMountedVehicle() as RidableHorse;
                //apply horse breed and charge Economics
                horse.ApplyBreed(breed);
                Player.Message(player, $"Your horse breed is now {breedName}");
                Puts($"{player.displayName}'s horse breed has been changed to {breedName}.");
                return;
            }

        }

        private bool IsValidBreed(string option, out int breed, out string breedName, out double cost)
        {
            if (option == "0" || option.Contains("appalosa"))
            {
                breed = 0;
                breedName = "Appalosa";
                cost = _config.costs.cost0;
                return true;
            }
            if (option == "1" || option.Contains("bay"))
            {
                breed = 1;
                breedName = "Bay";
                cost = _config.costs.cost1;
                return true;
            }
            if (option == "2" || option.Contains("buckskin"))
            {
                breed = 2;
                breedName = "Buckskin";
                cost = _config.costs.cost2;
                return true;
            }
            if (option == "3" || option.Contains("chestnut"))
            {
                breed = 3;
                breedName = "Chestnut";
                cost = _config.costs.cost3;
                return true;
            }
            if (option == "4" || option.Contains("dapple"))
            {
                breed = 4;
                breedName = "Dapple Grey";
                cost = _config.costs.cost4;
                return true;
            }
            if (option == "5" || option.Contains("piebald"))
            {
                breed = 5;
                breedName = "Piebald";
                cost = _config.costs.cost5;
                return true;
            }
            if (option == "6" || option.Contains("pinto"))
            {
                breed = 6;
                breedName = "Pinto";
                cost = _config.costs.cost6;
                return true;
            }
            if (option == "7" || option.Contains("red"))
            {
                breed = 7;
                breedName = "Red Roan";
                cost = _config.costs.cost7;
                return true;
            }
            if (option == "8" || option.Contains("white"))
            {
                breed = 8;
                breedName = "White Thoroughbred";
                cost = _config.costs.cost8;
                return true;
            }
            if (option == "9" || option.Contains("black"))
            {
                breed = 9;
                breedName = "Black Thoroughbred";
                cost = _config.costs.cost9;
                return true;
            }
            breed = 10;
            breedName = null;
            cost = 0;
            return false;
        }

        #endregion
    }
}
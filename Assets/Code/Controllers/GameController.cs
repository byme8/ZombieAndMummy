﻿using Assets.Code.Data;
using Assets.Code.Spawner;
using Assets.Code.Spawners;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Assets.Code.Seralization;

namespace Assets.Code.Controllers
{
    [RequireComponent(typeof(LevelCreator))]
    [RequireComponent(typeof(CoinSpawner))]
    [RequireComponent(typeof(CoinController))]
    [RequireComponent(typeof(PlayerSpawner))]
    [RequireComponent(typeof(UserInputController))]
    [RequireComponent(typeof(ZombieSpawner))]
    public class GameController : MonoBehaviour
    {
        public GameObject LevelRoot;
        public Camera Camera;

        public CoinController CoinController;
        public ZombieSpawner ZombieSpawner;
        public CoinSpawner CoinSpawner;
        public UserInputController UserInputController;
        public PlayerSpawner PayerController;
        public LevelCreator LevelCreator;
        public MenuController MainMenu;

        private float Time;

        void Start()
        {
            this.LevelCreator = this.GetComponent<LevelCreator>();
            this.PayerController = this.GetComponent<PlayerSpawner>();
            this.UserInputController = this.GetComponent<UserInputController>();
            this.CoinSpawner = this.GetComponent<CoinSpawner>();
            this.CoinController = this.GetComponent<CoinController>();
            this.ZombieSpawner = this.GetComponent<ZombieSpawner>();
            this.ZombieSpawner.LevelRoot = this.LevelRoot.transform;
            this.Camera = UnityEngine.Camera.main;

            this.StartGame();
        }

        public void StartGame()
        {
            this.ClearLevel();

            var level = this.LevelCreator.CreateLevel(this.LevelRoot, 30);
            var player = this.PayerController.CreatePlayer(this.LevelRoot, level);
            this.Camera.transform.parent = player.transform;
            this.Camera.transform.localPosition = new Vector3(0, 0, -10);

            this.ZombieSpawner.Level = level;
            this.CoinSpawner.StartCoinGeneration();
            this.CoinSpawner.GroundCells = level.GroundCells;
            this.UserInputController.Player = player.GetComponent<PlayerEntity>();

            this.Time = UnityEngine.Time.time;
            this.StartCoroutine(this.StartStoryCoroutine(level, player));
        }

        private void ClearLevel()
        {
            this.Camera.transform.parent = null;
            foreach (Transform child in this.LevelRoot.transform)
                GameObject.Destroy(child.gameObject);
        }

        private IEnumerator StartStoryCoroutine(Level level, PlayerEntity player)
        {
            yield return new WaitForSeconds(1);

            var firstZombie = this.ZombieSpawner.CreateZombie();
            firstZombie.StartWalk(level);

            yield return new WaitWhile(() => this.CoinController.Coins < 5);

            var secondZombie = this.ZombieSpawner.CreateZombie();
            secondZombie.StartWalk(level);

            yield return new WaitWhile(() => this.CoinController.Coins < 10);

            var mummy = this.ZombieSpawner.CreateMummy();
            mummy.StartWalk(level);

            yield return new WaitWhile(() => this.CoinController.Coins < 20);

            var zombies = new[] { firstZombie, secondZombie, mummy };

            foreach (var zombie in zombies)
            {
                zombie.StopWalking();
                zombie.StartFollowing(player);
            }

            var currentCoins = this.CoinController.Coins;

            while (true)
            {
                yield return new WaitWhile(() => this.CoinController.Coins == currentCoins);
                foreach (var zombie in zombies)
                    zombie.Speed *= 1.05f;

                currentCoins = this.CoinController.Coins;
            }
        }

        public void SaveResults(GameEndReason reason = GameEndReason.ZombieDeath)
        {
            using (var repository = new UserRepository())
            {
                repository.Records.Add(new Record
                {
                    Name = repository.User.Name,
                    Coins = this.CoinController.Coins,
                    Time = UnityEngine.Time.time - this.Time,
                    LaunchTime = DateTime.Now,
                    Reason = reason
                });
            }

            this.MainMenu.ShowRecords();
            this.ClearLevel();
            GameObject.Destroy(this.gameObject);
        }

        public void MummyEatPlayer()
        {
            this.CoinController.Coins = 0;
            this.SaveResults(GameEndReason.MummyDeath);
        }

        private void FixedUpdate()
        {
            if (Input.GetKey(KeyCode.Escape))
                this.SaveResults(GameEndReason.Quit);
        }
    }
}
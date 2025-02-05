﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using System.IO;

#if UNITY_EDITOR
using ParrelSync;
#endif

public enum CARDACTION
{
    Played, Dealt
}

public class PlayerManager : NetworkBehaviour
{
    private static HistoryTopic _topic = HistoryTopic.COMPUTER;

    public GameObject cardPrefab;

    private GameObject playerArea;
    private GameObject enemyAreas;
    private MPGameMessage messenger;
    private GameObject gameStateText;
    private GameObject activeSpecialAction;
    private GameObject deck;
    private GameObject dropzone;
    private MPTimer timer;
    private MPCanvasHUD menuCanvas;
    private MPQuestionManager questionManager;
    private TradingSystem tradingSystem;
    private PlayerStats playerStats;
    private EndGameAchievementChecker achievementChecker;

    [Header("Colors")]
    public Color normalTextColor = Color.white;
    public Color dangerTextColor = Color.red;

    public override void OnStartClient()
    {
        base.OnStartClient();

        playerArea = GameObject.Find("PlayerArea");
        enemyAreas = GameObject.Find("EnemyAreas");
        gameStateText = GameObject.Find("RoundText");
        activeSpecialAction = GameObject.Find("ActiveSpecialAction");
        deck = GameObject.Find("Deck");
        timer = GameObject.Find("MPTimer").GetComponent<MPTimer>();
        dropzone = GameObject.FindGameObjectWithTag("Timeline");
        menuCanvas = GameObject.Find("MenuCanvas").GetComponent<MPCanvasHUD>();
        messenger = GameObject.Find("MESSENGER").GetComponent<MPGameMessage>();
        questionManager = GameObject.Find("RoundQuestion").GetComponent<MPQuestionManager>();
        tradingSystem = GameObject.Find("TRADING").GetComponent<TradingSystem>();
        playerStats = GetComponent<PlayerStats>();
        achievementChecker = GameObject.Find("ENDGAMEACH").GetComponentInChildren<EndGameAchievementChecker>();
    }

    #region ------------------------------ LOCAL LOAD CARDS ------------------------------

    [ClientRpc]
    public void RpcSetTopic(HistoryTopic topic)
    {
        _topic = topic;
        tradingSystem.SetTopic(topic);
    }

    #endregion

    [Command(requiresAuthority = false)]
    public void CmdDrawCards(int _startingCardsCount)
    {
        for (int i = 0; i < _startingCardsCount; i++)
        {
            int infoIndex = MPGameManager.Instance.PopCard(netIdentity);
            SPECIALACTION randSpecial = MPGameManager.Instance.GetRandomSpecialAction();

            if (infoIndex < 0) break; // if deck has no cards

            GameObject card = Instantiate(cardPrefab, new Vector2(0, 0), Quaternion.identity);
            NetworkServer.Spawn(card, connectionToClient);
            RpcShowCard(card, infoIndex, -1, CARDACTION.Dealt, randSpecial);
        }
    }

    [Command]
    public void CmdReady(string playerName, int avatarIndex)
    {
        NetworkIdentity ni = connectionToClient.identity;
        MPGameManager.Instance.SetPlayerName(ni, playerName);
        MPGameManager.Instance.SetPlayerAvatar(ni, avatarIndex);
        MPGameManager.Instance.ReadyPlayer(ni);
    }

    [Command(requiresAuthority = false)]
    public void CmdGetAnotherCard()
    {
        int infoIndex = MPGameManager.Instance.PopCard(connectionToClient.identity);
        SPECIALACTION randSpecial = MPGameManager.Instance.GetRandomSpecialAction();

        if (infoIndex < 0) return; // if deck has no cards

        GameObject card = Instantiate(cardPrefab, new Vector2(0, 0), Quaternion.identity);
        NetworkServer.Spawn(card, connectionToClient);
        RpcShowCard(card, infoIndex, -1, CARDACTION.Dealt, randSpecial);
    }

    public void TradeCard(uint fromPlayer, int fromCard, uint toPlayer, int toCard)
    {
        CmdTradeCard(fromPlayer, fromCard, toPlayer, toCard);
        tradingSystem.SetupCanvas(false);
        timer.StopTimer();
    }

    [Command]
    public void CmdTradeCard(uint fromPlayer, int fromCard, uint toPlayer, int toCard)
    {
        MPGameManager.Instance.OnTradeCard(fromPlayer, fromCard, toPlayer, toCard);
    }

    [Command(requiresAuthority = false)]
    public void CmdGetSpecificCard(int infoIndex)
    {
        GameObject card = Instantiate(cardPrefab, new Vector2(0, 0), Quaternion.identity);
        NetworkServer.Spawn(card, connectionToClient);

        // No special action on traded cards
        RpcShowCard(card, infoIndex, -1, CARDACTION.Dealt, SPECIALACTION.None);
    }

    public void PlayCard(GameObject card, int infoIndex, int pos, bool hasDrop = true)
    {
        if (hasAuthority)
        {
            if (hasDrop)
            {
                CmdPlayCard(card, infoIndex, pos, hasDrop, card.GetComponent<MPCardInfo>().cardData.SpecialAction);
            }
            else
            {
                CmdPlayCard(card, infoIndex, pos, hasDrop, SPECIALACTION.None);
            }
        }

        tradingSystem.SetupCanvas(false);
        timer.StopTimer();
    }

    [Command]
    private void CmdPlayCard(GameObject card, int infoIndex, int pos, bool hasDrop, SPECIALACTION special)
    {
        bool isDropValid = MPGameManager.Instance.OnPlayCard(infoIndex, pos, netIdentity, hasDrop, special);

        if (hasDrop)
        {
            if (isDropValid)
            {
                // Show card only if card drop is right
                RpcShowCard(card, infoIndex, pos, CARDACTION.Played, special);
            }
            else
            {
                // Discard the dropped card
                NetworkConnection conn = card.GetComponent<NetworkIdentity>().connectionToClient;
                TargetDiscard(conn, card);
            }
        }

        TargetUpdateDropStats(isDropValid);
    }

    [TargetRpc]
    public void TargetUpdateDropStats(bool isDropValid)
    {
        Debug.Log($"STATS UPDATE: {isDropValid}");

        if (isDropValid)
            playerStats.CorrectDrop();
        else
            playerStats.IncorrectDrop();
    }

    [TargetRpc]
    private void TargetDiscard(NetworkConnection conn, GameObject card)
    {
        card.GetComponent<MPDragDrop>().OnDiscard(); // Animator.SetTrigger()
        StartCoroutine(DestroyObjectWithDelay(card));
    }

    [TargetRpc]
    public void TargetDiscardByInfoIndex(NetworkConnection conn, int cardToRemove)
    {
        foreach (Transform _card in playerArea.transform)
        {
            if (_card.GetComponent<MPCardInfo>().infoIndex == cardToRemove)
            {
                _card.GetComponent<MPDragDrop>().OnDiscard();
                StartCoroutine(DestroyObjectWithDelay(_card.gameObject));
                break;
            }
        }
    }

    #region ------------------------------ UPDATE GUI FUNCTIONS ------------------------------

    [ClientRpc]
    public void RpcUpdateUI(
        GameState gmState,
        byte[] _players,
        byte[] _playerHands,
        string qQuestion,
        string[] qChoices,
        string gameMsg,
        uint currentPlayerId,
        int deckCount,
        byte[] _winners,
        byte[] _playerTrades,
        byte[] _playerAvatars)
    {
        Dictionary<uint, string> players = CustomSerializer.DeserializePlayers(_players) ?? new Dictionary<uint, string>();
        Dictionary<uint, List<int>> playerHands = CustomSerializer.DeserializePlayerHands(_playerHands) ?? new Dictionary<uint, List<int>>();
        Dictionary<uint, string> winners = CustomSerializer.DeserializePlayers(_winners) ?? new Dictionary<uint, string>();
        Dictionary<uint, int> playerTrades = CustomSerializer.DeserializePlayerTrades(_playerTrades) ?? new Dictionary<uint, int>();
        Dictionary<uint, int> playerAvatars = CustomSerializer.DeserializePlayerTrades(_playerAvatars) ?? new Dictionary<uint, int>();

        winners.TryGetValue(NetworkClient.localPlayer.netId, out string _playerName);
        bool gameWon = _playerName != null;

        if (gmState == GameState.FINISHED)
        {
            menuCanvas.ShowEndGameMenu(winners, gameWon, false);
            EndGameUpdatePlayerStats(gameWon);
        }
        else
        {
            bool isMyTurn = NetworkClient.localPlayer.netId == currentPlayerId && gmState == GameState.STARTED;

            UpdateTurn(
                isMyTurn,
                gmState,
                qQuestion,
                qChoices,
                gameMsg,
                currentPlayerId);
            UpdateOpponents(
                isMyTurn,
                gmState,
                currentPlayerId,
                players,
                playerHands,
                playerTrades,
                playerAvatars);
            UpdateDeckCount(deckCount);
        }

    }

    public void UpdateTurn(
        bool isMyTurn,
        GameState gmState,
        string qQuestion,
        string[] qChoices,
        string gameMsg,
        uint currentPlayerId)
    {

        Debug.Log($"ILP: {isMyTurn}, GameState: {gmState}");

        // Disable IP Address when game already started
        if (gmState == GameState.STARTED)
        {
            try
            {
                GameObject.Find("IP-ADDRESS").SetActive(false);
            }
            catch { }
        }

        if (gmState == GameState.QUIZ && qQuestion != null && qChoices != null)
        {
            questionManager.ShowQuestion(qQuestion, qChoices);
            timer.StartQuizTimer();

            foreach (MPDragDrop dragDrop in FindObjectsOfType<MPDragDrop>())
            {
                dragDrop.DisableDrag();
            }
        }
        else
        {
            if (isMyTurn && gmState == GameState.STARTED)
            {
                timer.StartTimer();
                foreach (MPDragDrop dragDrop in playerArea.GetComponentsInChildren<MPDragDrop>())
                {
                    dragDrop.EnableDrag();
                }
            }
            else
            {
                foreach (MPDragDrop dragDrop in FindObjectsOfType<MPDragDrop>())
                {
                    dragDrop.DisableDrag();
                }
            }

            gameStateText.GetComponent<RoundText>().SetText(
            (isMyTurn && gmState == GameState.STARTED) ?
            $"{gameMsg} (Your Turn)" :
            gameMsg);
        }
    }

    [ClientRpc]
    public void RpcShowDoubleDraw(bool doubleDrawActive)
    {
        if (doubleDrawActive)
        {
            activeSpecialAction.GetComponentInChildren<TextMeshProUGUI>().text = "Next Player to make mistake gets double card!";
            activeSpecialAction.GetComponentInChildren<Image>().sprite = MPCardInfo.GetSpecialActionSprite(SPECIALACTION.DoubleDraw);
            activeSpecialAction.GetComponentInChildren<Image>().color = Color.white;
        }
        else
        {
            activeSpecialAction.GetComponentInChildren<TextMeshProUGUI>().text = "";
            activeSpecialAction.GetComponentInChildren<Image>().sprite = null;
            activeSpecialAction.GetComponentInChildren<Image>().color = Color.clear;
        }
    }

    public void UpdateOpponents(
        bool isMyTurn,
        GameState gmState,
        uint currentPlayerId,
        Dictionary<uint, string> players,
        Dictionary<uint, List<int>> playerHands,
        Dictionary<uint, int> playerTrades,
        Dictionary<uint, int> playerAvatars)
    {
        // Get Self
        uint _playerId = NetworkClient.localPlayer.netId;
        string _playerName = players[_playerId];
        int[] _playerHand = playerHands[_playerId].ToArray();
        int tradesLeft = playerTrades[_playerId];

        // Get Opponents
        IEnumerable<KeyValuePair<uint, string>> opponents = players.Where(p => p.Key != NetworkClient.localPlayer.netId);

        //Update UI
        for (int i = 0; i < enemyAreas.transform.childCount; i++)
        {
            Transform enemyGO = enemyAreas.transform.GetChild(i);

            AvatarLoader avatarGO = enemyGO.GetChild(0).GetChild(0).GetComponent<AvatarLoader>();
            TextMeshProUGUI cardCountGO = enemyGO.GetChild(1).Find("CARDS").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI playerNameGO = enemyGO.GetChild(1).Find("NAME").GetComponent<TextMeshProUGUI>();
            Button btnTrade = enemyGO.GetChild(2).GetChild(0).GetComponent<Button>();

            try
            {
                uint _opponentId = opponents.ElementAt(i).Key;
                string _opponentName = players[_opponentId];
                int[] _opponentHand = playerHands[_opponentId].ToArray();
                Avatar _opponentAvatar = (Avatar)playerAvatars[_opponentId];

                int handCount = _opponentHand.Length;
                string cardMsg = $"{handCount} Cards";

                playerNameGO.gameObject.GetComponent<TextEllipsisAnimation>().enabled = false;
                avatarGO.SetPreview(_opponentAvatar);

                if (gmState == GameState.STARTED)
                {
                    // Highlight current player if game started
                    cardMsg += _opponentId == currentPlayerId ? " (Playing)" : "";
                    cardCountGO.fontStyle = _opponentId == currentPlayerId ? FontStyles.Bold : FontStyles.Normal;

                    // Highlight player when only 1 card remaining
                    cardCountGO.color = handCount <= 1 ? dangerTextColor : normalTextColor;
                }

                playerNameGO.text = _opponentName;
                cardCountGO.text = cardMsg;

                // Attach Trade Callback
                btnTrade.gameObject.SetActive(true);
                btnTrade.onClick.RemoveAllListeners();
                btnTrade.onClick.AddListener(() => StartTrade(
                    _playerId, _playerName, _playerHand,
                    _opponentId, _opponentName, _opponentHand, tradesLeft));

                // Enable trading only when my turn and has trades left
                if (tradesLeft <= 0)
                {
                    btnTrade.interactable = false;
                    btnTrade.GetComponentInChildren<TextMeshProUGUI>().text = "NO TRADES LEFT";
                }
                else
                {
                    btnTrade.interactable = isMyTurn;
                    btnTrade.GetComponentInChildren<TextMeshProUGUI>().text = "TRADE";
                }
            }
            catch
            {
                if (gmState != GameState.WAITING)
                {
                    playerNameGO.gameObject.GetComponent<TextEllipsisAnimation>().enabled = false;
                    playerNameGO.text = "";
                    cardCountGO.text = "";
                }
                else
                {
                    playerNameGO.text = "Waiting For Players";
                    playerNameGO.gameObject.GetComponent<TextEllipsisAnimation>().enabled = true;
                }

                btnTrade.gameObject.SetActive(false);
            }
        }
    }

    public void StartTrade(
        uint playerId, string playerName, int[] playerHand,
        uint opponentId, string opponentName, int[] opponentHand,
        int tradesLeft)
    {
        SoundManager.Instance.PlayClickedSFX();

        tradingSystem.ShowTrade(
            playerId, playerName, playerHand,
            opponentId, opponentName, opponentHand,
            tradesLeft);
    }

    public void UpdateDeckCount(int deckCount)
    {
        deck.GetComponentInChildren<TextMeshProUGUI>().text = $"{deckCount} Cards Remaining";
    }

    [ClientRpc]
    public void RpcShowCard(GameObject card, int infoIndex, int pos, CARDACTION type, SPECIALACTION special)
    {
        card.GetComponent<MPCardInfo>().InitCardData(ResourceParser.Instance.GetCard(infoIndex, _topic), special);
        card.GetComponent<MPCardInfo>().infoIndex = infoIndex;
        card.GetComponent<MPDragDrop>().DisableDrag();

        if (type == CARDACTION.Dealt)
        {
            if (hasAuthority)
            {
                card.transform.SetParent(playerArea.transform, false);
            }
        }
        else if (type == CARDACTION.Played)
        {
            card.transform.SetParent(dropzone.transform, false);
            card.transform.SetSiblingIndex(pos);
            card.GetComponent<MPCardInfo>().RevealCard();
        }
    }

    [ClientRpc]
    public void RpcGameInterrupted(string playerName, bool isHost)
    {
        menuCanvas.ShowInterruptedGame(playerName, isHost);
    }

    [TargetRpc]
    public void TargetPeekCard(NetworkConnection conn)
    {
        Debug.Log("Activate Peek");

        int cardCount = playerArea.transform.childCount;

        if (cardCount > 0)
        {
            int randIndex = UnityEngine.Random.Range(0, cardCount);
            playerArea.transform.GetChild(randIndex).GetComponent<MPCardInfo>().RevealCard();
        }
    }

    [TargetRpc]
    public void TargetDiscardRandomHand(NetworkConnection conn, int infoIndex)
    {
        string cardIdToRemove = ResourceParser.Instance.GetCard(infoIndex, _topic).ID;

        for (int i = 0; i < playerArea.transform.childCount; i++)
        {
            GameObject card = playerArea.transform.GetChild(i).gameObject;

            if (card.GetComponent<MPCardInfo>().cardData.ID == cardIdToRemove)
            {
                card.GetComponent<MPDragDrop>().OnDiscard();
                StartCoroutine(DestroyObjectWithDelay(card));
                break;
            }
        }
    }



    [ClientRpc]
    public void RpcShowGameMessage(string message, MPGameMessageType type)
    {
        messenger.ShowMessage(message, type);
    }

    [TargetRpc]
    public void TargetShowQuizResult(NetworkConnection conn, string message, MPGameMessageType type)
    {
        messenger.ShowMessage(message, type);
        questionManager.SetVisibility(false);
    }
    #endregion

    #region ------------------------------ UTILS ------------------------------

    IEnumerator DestroyObjectWithDelay(GameObject obj)
    {
        yield return new WaitForSeconds(1f);
        NetworkServer.Destroy(obj);
    }

    #endregion

    #region ------------------------------ QUESTION MANAGER ------------------------------

    public void AnswerQuiz(string answer)
    {
        CmdAnswerQuiz(answer);
        timer.StopTimer();
    }

    [Command]
    public void CmdAnswerQuiz(string answer)
    {
        MPGameManager.Instance.OnAnswerQuiz(netIdentity, answer);
    }

    #endregion

    public void EndGameUpdatePlayerStats(bool gameWon)
    {
        Debug.Log("GAMEWON? " + gameWon);

#if UNITY_EDITOR
        if (Application.isEditor)
        {
            if (!ClonesManager.IsClone())
            {
                Debug.Log("END GAME: UPDATE STATS EDITOR");

                StaticData.Instance.profileStatisticsData.UpdateMPGameAccuracy(
                _topic, playerStats.Accuracy, gameWon);
            }
        }
        else
        {
            StaticData.Instance.profileStatisticsData.UpdateMPGameAccuracy(
            _topic, playerStats.Accuracy, gameWon);
        }
#else
        StaticData.Instance.profileStatisticsData.UpdateMPGameAccuracy(
                    _topic, playerStats.Accuracy, gameWon);
#endif

        achievementChecker.ViewAcquiredAchievements(new GameData(GameMode.MP, GameDifficulty.EASY, gameWon, playerStats.Accuracy, -1, -1));
    }
}

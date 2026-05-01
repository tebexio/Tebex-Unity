using System;
using System.Collections;
using System.Collections.Generic;
using Tebex.Headless;
using UnityEngine;

namespace Tebex.TebexUnity
{
    public class Deliverables : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] public string StorePublicKey;
        [SerializeField] private float pollIntervalSeconds = 5f;

        public Basket ActiveBasket { get; private set; }

        // Tracks completed baskets
        private HashSet<string> _appliedBaskets = new();

        // Tracks completed packages per basket (prevents partial execution bugs)
        private Dictionary<string, HashSet<int>> _appliedPackages = new();

        // Supports multiple actions per package
        private Dictionary<int, List<Action<BasketPackage>>> _actions = new();

        private bool _isProcessing;
        private Coroutine _pollRoutine;
        
        private void Start()
        {
            if (!string.IsNullOrEmpty(StorePublicKey))
            {
                HeadlessApi.GetInstance(new UnityHeadlessAdapter(), StorePublicKey);
            }

            StartPolling();
        }

        private void OnDisable()
        {
            StopPolling();
        }

        #region Registration

        public void RegisterDeliverableAction(Package package, Action<BasketPackage> callback)
        {
            if (package == null || callback == null) return;
            RegisterDeliverableAction(package.id, callback);
        }

        public void RegisterDeliverableAction(int packageId, Action<BasketPackage> callback)
        {
            if (callback == null) return;

            if (!_actions.ContainsKey(packageId))
                _actions[packageId] = new List<Action<BasketPackage>>();

            _actions[packageId].Add(callback);
        }

        public void ClearActions()
        {
            _actions.Clear();
        }

        #endregion

        #region Basket Lifecycle

        public void SetActiveBasket(Basket basket)
        {
            ActiveBasket = basket;
        }

        private void StartPolling()
        {
            if (_pollRoutine == null)
                _pollRoutine = StartCoroutine(PollRoutine());
        }

        private void StopPolling()
        {
            if (_pollRoutine != null)
            {
                StopCoroutine(_pollRoutine);
                _pollRoutine = null;
            }
        }

        private IEnumerator PollRoutine()
        {
            while (true)
            {
                CheckActiveBasket();
                yield return new WaitForSeconds(pollIntervalSeconds);
            }
        }

        #endregion

        #region Core Logic

        public void CheckActiveBasket()
        {
            if (ActiveBasket == null || _isProcessing)
                return;

            try
            {
                var basket = HeadlessApi.GetInstance().GetBasket(ActiveBasket.ident);

                if (basket == null)
                {
                    // Basket invalid / expired
                    ActiveBasket = null;
                    return;
                }

                ActiveBasket = basket;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error checking active basket: {e}");
                return;
            }

            CheckBasketDeliverables(ActiveBasket);
        }

        public void CheckBasketDeliverables(Basket basket)
        {
            if (basket == null) return;
            if (!basket.complete) return;

            if (_appliedBaskets.Contains(basket.ident))
                return;

            if (basket.packages == null || basket.packages.Count == 0)
                return;

            if (_isProcessing)
                return;

            _isProcessing = true;

            if (!_appliedPackages.ContainsKey(basket.ident))
                _appliedPackages[basket.ident] = new HashSet<int>();

            var appliedSet = _appliedPackages[basket.ident];

            //TODO check if applied via tebex inventory so things aren't duplicated

            bool allSucceeded = true;

            foreach (var basketPackage in basket.packages)
            {
                if (basketPackage == null)
                    continue;

                // Skip already processed package
                if (appliedSet.Contains(basketPackage.id))
                    continue;

                if (!_actions.TryGetValue(basketPackage.id, out var actionList))
                {
                    allSucceeded = false;

                    //TODO alert on no action for package
                    continue;
                }

                bool packageSuccess = true;

                foreach (var action in actionList)
                {
                    try
                    {
                        action?.Invoke(basketPackage);
                    }
                    catch (Exception e)
                    {
                        packageSuccess = false;
                        allSucceeded = false;
                        Debug.LogError($"Error executing action for package {basketPackage.id}: {e}");
                    }
                }

                if (packageSuccess)
                {
                    appliedSet.Add(basketPackage.id);
                }
            }

            // If ALL packages successfully processed we can mark the basket complete
            if (allSucceeded && appliedSet.Count == basket.packages.Count)
            {
                _appliedBaskets.Add(basket.ident);

                //TODO set basket completed in tebex inventory

                ActiveBasket = null;
            }

            _isProcessing = false;
        }

        #endregion
    }
}
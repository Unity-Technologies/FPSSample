using UnityEngine;

namespace Macrometa {
   
    public class GDNErrorhandler {
        
        public int increasePauseConnectionError = 10;
        public bool isWaitingDebug = false;
        private bool _isWaiting;
        private int _currentNetworkErrors;

        public float pauseNetworkError = 1f;
        public float basePauseNetworkError = 1f;
        public float pauseNetworkErrorMultiplier = 2f;
        public float pauseNetworkErrorUntil = 0f;

        public bool isWaiting {
            get => _isWaiting;
            set {
                _isWaiting = value;
                if (isWaitingDebug) {
                    GameDebug.Log("isWaiting: " + value);
                }
            }
        }

        public int currentNetworkErrors {
            get => _currentNetworkErrors;
            set {
                _currentNetworkErrors = value;
                if (currentNetworkErrors == 0) {
                    pauseNetworkError = basePauseNetworkError;
                }
                if (isWaitingDebug) {
                    GameDebug.Log("NetworkError: " + value);
                    pauseNetworkErrorUntil = Time.time + pauseNetworkError;
                }
            }
        }
    }
}
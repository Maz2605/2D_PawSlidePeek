using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using _PawSlidePopGame._Scripts.Core.Boostrap;
using _PawSlidePopGame._Scripts.Data.SaveSystem;
using _PawSlidePopGame._Scripts.Services.Auth;

#if FIREBASE_AUTH_ENABLED
using Firebase.Auth;
#endif

#if FIREBASE_FIRESTORE_ENABLED
using Firebase.Firestore;
using Firebase.Extensions;
#endif

namespace _PawSlidePopGame._Scripts.Services.Save
{
    [DisallowMultipleComponent]
    public class FirebaseSaveManager : MonoBehaviour, IAppService
    {
        public static FirebaseSaveManager Instance { get; private set; }
        public static bool IsInitialized { get; private set; }

        private bool _initStarted;

        public void Init()
        {
            if (_initStarted) return;
            _initStarted = true;
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSaveManager();
        }

        private void InitializeSaveManager()
        {
#if FIREBASE_AUTH_ENABLED && FIREBASE_FIRESTORE_ENABLED
            SaveSystem.OnSaveCompleted += HandleLocalSaveCompleted;
            
            if (FirebaseAuthService.Instance != null)
            {
                FirebaseAuthService.Instance.OnLoginSuccess += HandleLoginSuccess;
                if (FirebaseAuthService.Instance.IsLoggedIn)
                {
                    HandleLoginSuccess(FirebaseAuthService.Instance.CurrentUser);
                }
            }
            IsInitialized = true;
            Debug.Log("[FirebaseSaveManager] Initialized successfully with Firestore support.");
#else
            Debug.LogWarning("[FirebaseSaveManager] FIREBASE_FIRESTORE_ENABLED or FIREBASE_AUTH_ENABLED is not defined. Running in mock/local mode.");
            IsInitialized = true;
#endif
        }

#if FIREBASE_AUTH_ENABLED && FIREBASE_FIRESTORE_ENABLED
        private void HandleLocalSaveCompleted(string gameId, string rawJson)
        {
            if (FirebaseAuthService.Instance == null || !FirebaseAuthService.Instance.IsLoggedIn)
            {
                return;
            }

            string userId = FirebaseAuthService.Instance.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            string localPath = SaveSystem.GetPath(gameId);
            DateTime lastWriteTime = File.Exists(localPath) ? File.GetLastWriteTimeUtc(localPath) : DateTime.UtcNow;

            UploadToCloud(userId, gameId, rawJson, lastWriteTime);
        }

        private void HandleLoginSuccess(FirebaseUser user)
        {
            if (user == null) return;
            string userId = user.UserId;
            if (string.IsNullOrEmpty(userId)) return;

            Debug.Log($"[FirebaseSaveManager] Login success. Starting Cloud Save sync for UID: {userId}");
            SyncAllDataFromCloud(userId);
        }

        private void UploadToCloud(string userId, string gameId, string rawJson, DateTime timestamp)
        {
            try
            {
                FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
                DocumentReference docRef = db.Collection("users").Document(userId).Collection("data").Document(gameId);

                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    { "json", rawJson },
                    { "timestamp", timestamp.ToString("o") }
                };

                docRef.SetAsync(data).ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        Debug.LogError($"[FirebaseSaveManager] Failed to upload {gameId} to cloud: {task.Exception}");
                    }
                    else
                    {
                        Debug.Log($"[FirebaseSaveManager] Synced {gameId} to cloud. Timestamp: {timestamp.ToString("o")}");
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseSaveManager] Error during upload: {e.Message}");
            }
        }

        private void SyncAllDataFromCloud(string userId)
        {
            try
            {
                FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
                CollectionReference colRef = db.Collection("users").Document(userId).Collection("data");

                colRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        Debug.LogError($"[FirebaseSaveManager] Failed to fetch cloud saves: {task.Exception}");
                        return;
                    }

                    QuerySnapshot snapshot = task.Result;
                    if (snapshot == null) return;

                    foreach (DocumentSnapshot doc in snapshot.Documents)
                    {
                        string gameId = doc.Id;
                        if (!doc.ContainsField("json") || !doc.ContainsField("timestamp")) continue;

                        string cloudJson = doc.GetValue<string>("json");
                        string cloudTimeStr = doc.GetValue<string>("timestamp");

                        if (DateTime.TryParse(cloudTimeStr, out DateTime cloudTime))
                        {
                            cloudTime = cloudTime.ToUniversalTime();
                            string localPath = SaveSystem.GetPath(gameId);
                            bool localExists = File.Exists(localPath);
                            DateTime localTime = localExists ? File.GetLastWriteTimeUtc(localPath) : DateTime.MinValue;

                            // So sánh lệch thời gian lớn hơn 1.5 giây để tránh vòng lặp ghi đè do sai số hệ thống
                            if (!localExists || (cloudTime - localTime).TotalSeconds > 1.5f)
                            {
                                Debug.Log($"[FirebaseSaveManager] Cloud data for '{gameId}' is newer ({cloudTime.ToString("o")}) than local ({localTime.ToString("o")}). Syncing to local...");
                                SaveSystem.SaveRawJson(gameId, cloudJson);
                                
                                // Thiết lập thời gian ghi file local trùng khớp với cloud
                                if (File.Exists(localPath))
                                {
                                    File.SetLastWriteTimeUtc(localPath, cloudTime);
                                }
                            }
                            else if ((localTime - cloudTime).TotalSeconds > 1.5f)
                            {
                                Debug.Log($"[FirebaseSaveManager] Local data for '{gameId}' is newer ({localTime.ToString("o")}) than cloud ({cloudTime.ToString("o")}). Syncing to cloud...");
                                string localJson = SaveSystem.LoadRawJson(gameId);
                                if (!string.IsNullOrEmpty(localJson))
                                {
                                    UploadToCloud(userId, gameId, localJson, localTime);
                                }
                            }
                            else
                            {
                                Debug.Log($"[FirebaseSaveManager] '{gameId}' is in sync. Local: {localTime.ToString("o")}, Cloud: {cloudTime.ToString("o")}");
                            }
                        }
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseSaveManager] Error during sync: {e.Message}");
            }
        }
#endif

        private void OnDestroy()
        {
#if FIREBASE_AUTH_ENABLED && FIREBASE_FIRESTORE_ENABLED
            SaveSystem.OnSaveCompleted -= HandleLocalSaveCompleted;
            if (FirebaseAuthService.Instance != null)
            {
                FirebaseAuthService.Instance.OnLoginSuccess -= HandleLoginSuccess;
            }
#endif
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}

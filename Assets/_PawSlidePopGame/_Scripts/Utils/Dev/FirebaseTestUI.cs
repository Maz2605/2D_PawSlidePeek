using System;
using UnityEngine;
using _PawSlidePopGame._Scripts.Services.Auth;
using _PawSlidePopGame._Scripts.Services.Save;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager;

namespace _PawSlidePopGame._Scripts.Utils.Dev
{
    [DisallowMultipleComponent]
    public class FirebaseTestUI : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool showUI = true;
        [SerializeField] private string googleWebClientId = "398760079055-cai9m31jk678m2nac03je2051s5b7ehh.apps.googleusercontent.com";

        private bool _isExpanded = false;
        private string _statusText = "Ready";
        private string _testEmail = "testuser@gmail.com";
        private string _testPassword = "Password123";

        private void OnGUI()
        {
            if (!showUI) return;

            // Thiết lập font size phù hợp cho di động và editor
            GUI.skin.button.fontSize = 13;
            GUI.skin.label.fontSize = 13;
            GUI.skin.box.fontSize = 14;

            if (!_isExpanded)
            {
                if (GUI.Button(new Rect(15, 15, 160, 45), "★ Firebase Panel"))
                {
                    _isExpanded = true;
                }
                return;
            }

            // Vẽ panel mở rộng
            Rect windowRect = new Rect(15, 15, 330, 520);
            GUILayout.BeginArea(windowRect, "Firebase Auth & Cloud Save Test", GUI.skin.box);
            
            GUILayout.Space(25);
            
            // Nút đóng (X)
            if (GUI.Button(new Rect(295, 5, 30, 22), "X"))
            {
                _isExpanded = false;
            }

            // Trạng thái Đăng nhập hiện tại
            bool isLoggedIn = FirebaseAuthService.Instance != null && FirebaseAuthService.Instance.IsLoggedIn;
            string userId = FirebaseAuthService.Instance != null ? FirebaseAuthService.Instance.UserId : "Unknown";
            
            GUILayout.Label($"<b>User ID:</b> {userId}");
            GUILayout.Label($"<b>Auth Mode:</b> {(isLoggedIn ? "<color=green>Online (Linked)</color>" : "<color=orange>Offline (Anonymous)</color>")}");
            GUILayout.Label($"<b>Status Log:</b> {_statusText}");

            GUILayout.Space(10);

            // Nhóm chức năng Authentication
            GUILayout.Label("<b>Authentication:</b>");
            
            if (GUILayout.Button("Sign In / Link Google", GUILayout.Height(35)))
            {
                _statusText = "Starting Google Sign-In...";
                if (FirebaseAuthService.Instance != null)
                {
                    FirebaseAuthService.Instance.SignInWithGoogle(
                        googleWebClientId,
                        user => {
                            _statusText = $"<color=green>Google Link Success!</color> UID: {user.UserId}";
                        },
                        err => {
                            _statusText = $"<color=red>Google Link Failed:</color> {err}";
                        }
                    );
                }
            }

            if (GUILayout.Button("Sign In / Link Facebook", GUILayout.Height(35)))
            {
                _statusText = "Starting Facebook Sign-In...";
                if (FirebaseAuthService.Instance != null)
                {
                    FirebaseAuthService.Instance.SignInWithFacebook(
                        user => {
                            _statusText = $"<color=green>Facebook Link Success!</color> UID: {user.UserId}";
                        },
                        err => {
                            _statusText = $"<color=red>Facebook Link Failed:</color> {err}";
                        }
                    );
                }
            }

            GUILayout.Space(5);
            GUILayout.Label("<b>Editor Email Login (For Testing in Editor):</b>");
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Email:", GUILayout.Width(50));
            _testEmail = GUILayout.TextField(_testEmail);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Pass:", GUILayout.Width(50));
            _testPassword = GUILayout.PasswordField(_testPassword, '*', 30);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Register Email", GUILayout.Height(30)))
            {
                _statusText = "Registering email...";
                if (FirebaseAuthService.Instance != null)
                {
#if FIREBASE_AUTH_ENABLED
                    FirebaseAuthService.Instance.RegisterWithEmail(
                        _testEmail,
                        _testPassword,
                        user => {
                            _statusText = $"<color=green>Reg Success!</color> UID: {user.UserId}";
                        },
                        err => {
                            _statusText = $"<color=red>Reg Failed:</color> {err}";
                        }
                    );
#else
                    FirebaseAuthService.Instance.RegisterWithEmail(
                        _testEmail,
                        _testPassword,
                        uid => {
                            _statusText = $"<color=green>Reg Success (Mock)!</color> UID: {uid}";
                        },
                        err => {
                            _statusText = $"<color=red>Reg Failed:</color> {err}";
                        }
                    );
#endif
                }
            }

            if (GUILayout.Button("Login / Link Email", GUILayout.Height(30)))
            {
                _statusText = "Logging in email...";
                if (FirebaseAuthService.Instance != null)
                {
#if FIREBASE_AUTH_ENABLED
                    FirebaseAuthService.Instance.SignInWithEmail(
                        _testEmail,
                        _testPassword,
                        user => {
                            _statusText = $"<color=green>Login Success!</color> UID: {user.UserId}";
                        },
                        err => {
                            _statusText = $"<color=red>Login Failed:</color> {err}";
                        }
                    );
#else
                    FirebaseAuthService.Instance.SignInWithEmail(
                        _testEmail,
                        _testPassword,
                        uid => {
                            _statusText = $"<color=green>Login Success (Mock)!</color> UID: {uid}";
                        },
                        err => {
                            _statusText = $"<color=red>Login Failed:</color> {err}";
                        }
                    );
#endif
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (GUILayout.Button("Sign Out (Switch to Anonymous)", GUILayout.Height(30)))
            {
                if (FirebaseAuthService.Instance != null)
                {
                    FirebaseAuthService.Instance.SignOut();
                    _statusText = "Signed Out. Logging in anonymously...";
                    FirebaseAuthService.Instance.SignInAnonymously();
                }
            }

            GUILayout.Space(10);
            
            // Nhóm chức năng Sync dữ liệu
            GUILayout.Label("<b>Cloud Save Sync Test:</b>");

            if (GUILayout.Button("Add +100 Coins (Triggers Cloud Save)", GUILayout.Height(35)))
            {
                if (PlayerEconomyRepository.Instance != null)
                {
                    PlayerEconomyRepository.Instance.Data.coins += 100;
                    PlayerEconomyRepository.Instance.Save();
                    _statusText = $"<color=cyan>Coins saved: {PlayerEconomyRepository.Instance.Data.coins}</color> (Sync triggered)";
                }
                else
                {
                    _statusText = "<color=red>PlayerEconomyRepository not found</color>";
                }
            }

            if (GUILayout.Button("Unlock Next Level (Triggers Cloud Save)", GUILayout.Height(35)))
            {
                if (LevelProgressRepository.Instance != null)
                {
                    int currentHighest = LevelProgressRepository.Instance.GetHighestUnlockedLevelNumber();
                    LevelProgressRepository.Instance.Data.highestUnlockedLevelNumber = currentHighest + 1;
                    LevelProgressRepository.Instance.Save();
                    _statusText = $"<color=cyan>Unlocked Level {currentHighest + 1}</color> (Sync triggered)";
                }
                else
                {
                    _statusText = "<color=red>LevelProgressRepository not found</color>";
                }
            }

            GUILayout.EndArea();
        }
    }
}

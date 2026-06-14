#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using _PawSlidePopGame._Scripts.Services.Auth;
using _PawSlidePopGame._Scripts.Services.Save;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using _PawSlidePopGame._Scripts.Gameplay.Meta.MapManager;

namespace _PawSlidePopGame._Scripts.Utils.Dev
{
    public class DevTestFirebase : EditorWindow
    {
        [MenuItem("DevTestFirebase/Open Panel")]
        public static void ShowWindow()
        {
            GetWindow<DevTestFirebase>("DevTestFirebase");
        }

        private string _statusText = "Ready";
        private string _testEmail = "testuser@gmail.com";
        private string _testPassword = "Password123";
        private string _googleWebClientId = "398760079055-cai9m31jk678m2nac03je2051s5b7ehh.apps.googleusercontent.com";
        private Vector2 _scrollPos;

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Please Enter Play Mode in Unity Editor to test Firebase Auth & Cloud Save.", MessageType.Warning);
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // Title
            GUILayout.Label("Firebase Editor Test Panel", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Trạng thái Đăng nhập hiện tại
            bool isLoggedIn = FirebaseAuthService.Instance != null && FirebaseAuthService.Instance.IsLoggedIn;
            string userId = FirebaseAuthService.Instance != null ? FirebaseAuthService.Instance.UserId : "Unknown";

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("User ID:", userId, EditorStyles.boldLabel);
            string authMode = isLoggedIn ? "Online (Linked)" : "Offline (Anonymous)";
            EditorGUILayout.LabelField("Auth Mode:", authMode, EditorStyles.boldLabel);
            
            // Rich status log text
            GUIStyle logStyle = new GUIStyle(EditorStyles.label);
            logStyle.richText = true;
            EditorGUILayout.LabelField("Status Log:", _statusText, logStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Nhóm chức năng Authentication
            GUILayout.Label("Authentication Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            if (GUILayout.Button("Sign In / Link Google", GUILayout.Height(30)))
            {
                _statusText = "Starting Google Sign-In...";
                if (FirebaseAuthService.Instance != null)
                {
                    FirebaseAuthService.Instance.SignInWithGoogle(
                        _googleWebClientId,
                        user => {
                            _statusText = $"<color=green>Google Link Success!</color> UID: {user.UserId}";
                            Repaint();
                        },
                        err => {
                            _statusText = $"<color=red>Google Link Failed:</color> {err}";
                            Repaint();
                        }
                    );
                }
            }

            if (GUILayout.Button("Sign In / Link Facebook", GUILayout.Height(30)))
            {
                _statusText = "Starting Facebook Sign-In...";
                if (FirebaseAuthService.Instance != null)
                {
                    FirebaseAuthService.Instance.SignInWithFacebook(
                        user => {
                            _statusText = $"<color=green>Facebook Link Success!</color> UID: {user.UserId}";
                            Repaint();
                        },
                        err => {
                            _statusText = $"<color=red>Facebook Link Failed:</color> {err}";
                            Repaint();
                        }
                    );
                }
            }

            EditorGUILayout.Space();
            GUILayout.Label("Editor Email Login (Works in PC Editor)", EditorStyles.miniBoldLabel);
            _testEmail = EditorGUILayout.TextField("Email", _testEmail);
            _testPassword = EditorGUILayout.PasswordField("Password", _testPassword);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Register Email", GUILayout.Height(25)))
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
                            Repaint();
                        },
                        err => {
                            _statusText = $"<color=red>Reg Failed:</color> {err}";
                            Repaint();
                        }
                    );
#else
                    FirebaseAuthService.Instance.RegisterWithEmail(
                        _testEmail,
                        _testPassword,
                        uid => {
                            _statusText = $"<color=green>Reg Success (Mock)!</color> UID: {uid}";
                            Repaint();
                        },
                        err => {
                            _statusText = $"<color=red>Reg Failed:</color> {err}";
                            Repaint();
                        }
                    );
#endif
                }
            }

            if (GUILayout.Button("Login / Link Email", GUILayout.Height(25)))
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
                            Repaint();
                        },
                        err => {
                            _statusText = $"<color=red>Login Failed:</color> {err}";
                            Repaint();
                        }
                    );
#else
                    FirebaseAuthService.Instance.SignInWithEmail(
                        _testEmail,
                        _testPassword,
                        uid => {
                            _statusText = $"<color=green>Login Success (Mock)!</color> UID: {uid}";
                            Repaint();
                        },
                        err => {
                            _statusText = $"<color=red>Login Failed:</color> {err}";
                            Repaint();
                        }
                    );
#endif
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (GUILayout.Button("Sign Out (Switch to Anonymous)", GUILayout.Height(30)))
            {
                if (FirebaseAuthService.Instance != null)
                {
                    FirebaseAuthService.Instance.SignOut();
                    _statusText = "Signed Out. Logging in anonymously...";
                    FirebaseAuthService.Instance.SignInAnonymously();
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Nhóm chức năng Sync dữ liệu
            GUILayout.Label("Cloud Save Sync Test", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            if (GUILayout.Button("Add +100 Coins (Triggers Cloud Save)", GUILayout.Height(30)))
            {
                if (PlayerEconomyRepository.Instance != null)
                {
                    PlayerEconomyRepository.Instance.Data.coins += 100;
                    PlayerEconomyRepository.Instance.Save();
                    _statusText = $"<color=blue>Coins saved: {PlayerEconomyRepository.Instance.Data.coins}</color> (Sync triggered)";
                }
                else
                {
                    _statusText = "<color=red>PlayerEconomyRepository not found</color>";
                }
            }

            if (GUILayout.Button("Unlock Next Level (Triggers Cloud Save)", GUILayout.Height(30)))
            {
                if (LevelProgressRepository.Instance != null)
                {
                    int currentHighest = LevelProgressRepository.Instance.GetHighestUnlockedLevelNumber();
                    LevelProgressRepository.Instance.Data.highestUnlockedLevelNumber = currentHighest + 1;
                    LevelProgressRepository.Instance.Save();
                    _statusText = $"<color=blue>Unlocked Level {currentHighest + 1}</color> (Sync triggered)";
                }
                else
                {
                    _statusText = "<color=red>LevelProgressRepository not found</color>";
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        private void OnInspectorUpdate()
        {
            // Repaint continuously during play mode to update display variables
            if (EditorApplication.isPlaying)
            {
                Repaint();
            }
        }
    }
}
#endif

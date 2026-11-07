using System;
using Cysharp.Threading.Tasks;
using Memories.Gameplay.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Memories.Bootstrap
{
    // Runtime UI: deliberately available in builds, independent of the developer console.
    public sealed class TesterCheckpointUI : MonoBehaviour
    {
        private GameSession session;
        private GameplayModeState pausedState;
        private bool visible, loading;
        private string error;
        public bool IsVisible => visible;
        public void Initialize(GameSession value) => session = value;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame) Toggle();
            if (visible && !loading) PauseGameplay();
        }

        public void Toggle()
        {
            if (loading) return;
            visible = !visible;
            error = null;
            if (visible) PauseGameplay(); else ReleaseGameplay();
        }

        private void PauseGameplay()
        {
            if (session?.LocationScope == null) return;
            var state = session.LocationScope.Container.Resolve<GameplayModeState>();
            if (state.Mode == GameplayMode.Playing && state.TryEnter(GameplayMode.Tester)) pausedState = state;
        }

        private void ReleaseGameplay()
        {
            pausedState?.Exit(GameplayMode.Tester);
            pausedState = null;
        }

        private async UniTask Select(TesterCheckpoint checkpoint)
        {
            if (loading || session == null || session.IsTransitioning) return;
            loading = true;
            error = null;
            ReleaseGameplay();
            try
            {
                await session.JumpToCheckpointAsync(checkpoint);
                visible = false;
            }
            catch (Exception exception)
            {
                error = "Не удалось перейти: " + exception.Message;
                Debug.LogException(exception);
            }
            finally { loading = false; if (visible) PauseGameplay(); }
        }

        private void OnGUI()
        {
            if (!visible) return;
            float scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            var matrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            GUI.depth = -1000;
            GUI.Box(new Rect(0, 0, width, height), "");
            GUILayout.BeginArea(new Rect((width - 680) / 2, (height - 540) / 2, 680, 540), GUI.skin.window);
            GUILayout.Space(12);
            GUILayout.Label("ТЕСТЕР · КОНТРОЛЬНЫЕ ТОЧКИ", new GUIStyle(GUI.skin.label) { fontSize = 23, alignment = TextAnchor.MiddleCenter });
            GUILayout.Label("F8 — открыть / закрыть", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
            GUILayout.Space(12);
            GUILayout.Label("Каждый переход заново восстанавливает этап игры.\nТестовый прогресс не перезаписывает обычное сохранение.", new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 16 });
            GUILayout.Space(16);
            GUI.enabled = !loading && session != null && !session.IsTransitioning;
            var button = new GUIStyle(GUI.skin.button) { fontSize = 17, wordWrap = true };
            if (GUILayout.Button("1. Вход в дом\nНичего не подобрано и не активировано", button, GUILayout.Height(64))) Select(TesterCheckpoint.HouseEntrance).Forget();
            if (GUILayout.Button("2. После воспоминания в доме\nТелевизор включён, выход во двор открыт", button, GUILayout.Height(64))) Select(TesterCheckpoint.HouseMemoryComplete).Forget();
            if (GUILayout.Button("3. После второго воспоминания во дворе\nОкно разбито, гараж подгружен и открыт", button, GUILayout.Height(64))) Select(TesterCheckpoint.BackyardMemoryComplete).Forget();
            if (GUILayout.Button("4. Двери гаража открыты в коридор\nФинал гаража завершён, проход в спортзал готов", button, GUILayout.Height(64))) Select(TesterCheckpoint.GarageCorridorOpen).Forget();
            GUI.enabled = !loading;
            GUILayout.Space(10);
            if (GUILayout.Button("Закрыть", GUILayout.Height(30))) Toggle();
            GUI.enabled = true;
            if (loading) GUILayout.Label("Восстанавливаем контрольную точку…");
            else if (!string.IsNullOrEmpty(error)) GUILayout.Label(error, new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUILayout.EndArea();
            GUI.matrix = matrix;
        }
        private void OnDestroy() => ReleaseGameplay();
    }
}

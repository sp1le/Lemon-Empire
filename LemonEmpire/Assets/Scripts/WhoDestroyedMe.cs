using UnityEngine;
public class WhoDestroyedMe : MonoBehaviour
{
    private Vector3 _lastPos;
    private void Update()
    {
        _lastPos = transform.position;
        // Если игрок улетел слишком далеко - мы это заметим еще до удаления
        if (transform.position.y < -30f) {
            Debug.LogWarning($"<color=orange>[ALARM] Игрок падает! Y = {transform.position.y}</color>");
        }
    }
    private void OnDestroy()
    {
        // Самый важный лог
        Debug.Log($"<color=cyan>[DEBUG] Объект {gameObject.name} УДАЛЕН!</color>");
        Debug.Log($"Последняя позиция перед смертью: {_lastPos}");
        
        // Пробуем достать причину через более глубокий стек
        string stack = UnityEngine.StackTraceUtility.ExtractStackTrace();
        Debug.Log($"Полный стек вызова:\n {stack}");
        if (_lastPos.y < -50f) {
            Debug.LogError("!!! ВНИМАНИЕ: Твой игрок провалился под карту и был удален движком. Проверь коллизии пола!");
        }
    }
}
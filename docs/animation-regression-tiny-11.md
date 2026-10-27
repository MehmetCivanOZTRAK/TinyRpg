# TINY-11 — Karakter animasyon regresyonu

## Statik inceleme

| Karakter | Oyun prefabı | Animator | Parametreler | Ekipman çıkışı |
| --- | --- | --- | --- | --- |
| Warrior | `Assets/Prefabs/Warrior.prefab` | `Assets/Animation/Warrior.controller` | `isRunning` (bool), `Attack` (trigger) | `SwordHitOrigin` |
| Archer | `Assets/Prefabs/Archer.prefab` | `Assets/Animation/Archer.controller` | `isRunning` (bool), `Attack` (trigger) | `ArrowSpawnOrigin` |
| Mage | `Assets/Prefabs/Mage.prefab` | `Assets/Animation/Mage.controller` | `isRunning` (bool), `Attack` (trigger) | `StaffCastOrigin`, `OffhandCastOrigin` |

Üç Animator `Idle` ve `Run` durumlarını içeriyor. Warrior saldırıdan sonra `Idle` durumuna, Archer ve Mage ise `Attack` → `Release` → `Idle` durumlarına geçiyor. `PlayerController` yalnızca `isRunning` parametresini yazıyor; saldırı tetikleyicisini kullanmıyor. Bu görev saldırı kontrolü veya takım komutları eklemiyor.

`GameScene.unity` içindeki `Player` nesnesinde `PlayerController`, `NavMeshAgent` ve Animator var. `arrowparticle` alanı sahnedeki Particle System'e bağlı. `Main Camera` etiketi mevcut ve `CameraFollow.target` aynı `Player` kökünün Transform'una bağlı. `PlayerController` ve `CameraFollow` dosyaları bu görevde değiştirilmedi.

## Unity doğrulaması

| Kontrol | Sonuç |
| --- | --- |
| Prefab/Animator/Avatar referansları | Bekliyor |
| Idle ↔ Run, Attack/Release geçişleri | Bekliyor |
| GameScene Play Mode tek karakter hareketi | Bekliyor |
| Kamera takip davranışı | Bekliyor |
| EditMode Test Runner | Bekliyor |
| PlayMode Test Runner | Bekliyor |
| Derleme ve konsol | Bekliyor |

Unity Editor çalışma ağacındaki önceki oturumun sahne yedeği için `Recovering Scene Backups` penceresinde duruyor. Yedek `Assets/_Recovery` içine korunup Editor açıldıktan sonra bu tablo gerçek sonuçlarla güncellenecek. Statik inceleme Play Mode doğrulaması sayılmaz.

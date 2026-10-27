# TINY-11 — Karakter animasyon regresyonu

## Statik inceleme

| Karakter | Oyun prefabı | Animator | Parametreler | Ekipman çıkışı |
| --- | --- | --- | --- | --- |
| Warrior | `Assets/Prefabs/Warrior.prefab` | `Assets/Animation/Warrior.controller` | `isRunning` (bool), `Attack` (trigger) | `SwordHitOrigin` |
| Archer | `Assets/Prefabs/Archer.prefab` | `Assets/Animation/Archer.controller` | `isRunning` (bool), `Attack` (trigger) | `ArrowSpawnOrigin` |
| Mage | `Assets/Prefabs/Mage.prefab` | `Assets/Animation/Mage.controller` | `isRunning` (bool), `Attack` (trigger) | `StaffCastOrigin`, `OffhandCastOrigin` |

Üç Animator `Idle` ve `Run` durumlarını içeriyor. Warrior saldırıdan sonra `Idle` durumuna, Archer ve Mage ise `Attack` → `Release` → `Idle` durumlarına geçiyor. `PlayerController` yalnızca `isRunning` parametresini yazıyor; saldırı tetikleyicisini kullanmıyor. Bu görev saldırı kontrolü veya takım komutları eklemiyor.

`GameScene.unity` içindeki `Player` nesnesinde `PlayerController`, `NavMeshAgent` ve Animator var. `arrowparticle` alanı sahnedeki Particle System'e bağlı. `Main Camera` etiketi mevcut ve `CameraFollow.target` aynı `Player` kökünün Transform'una bağlı. `PlayerController` ve `CameraFollow` dosyaları bu görevde değiştirilmedi.

## Giderilen regresyonlar

- Warrior ve Archer `Run` klipleri `FootR`/`FootL` olaylarını çağırıyordu, fakat oyun prefablarında olay alıcısı yoktu. Projede zaten bulunan `WarriorCharacterAnimatorEvents` bileşeni iki prefaba ve sahnedeki `Player` nesnesine eklendi. Mage prefabında bu bileşen zaten vardı.
- `GameScene` içindeki `Player`, Warrior prefabından bağımsız bir sahne kopyasıydı. Önceki prefab düzeltmesini miras almadığından materyali eksik sabit kılıcı gösteriyordu. Sahne kopyasında sağ el rigindeki kılıç açıldı, sabit kopya kaldırıldı ve `SwordHitOrigin` eklendi. Kamera hedefi, tıklama kontrolü, NavMeshAgent ve parçacık referansı korundu.

## Unity doğrulaması

| Kontrol | Sonuç |
| --- | --- |
| Prefab/Animator/Avatar referansları | Unity 6000.3.12f1'de üç Avatar geçerli; üç controller ve parametreleri bağlı. |
| Idle ↔ Run, Attack/Release geçişleri | Play Mode'da üç karakter Run ve Idle'a geçti; Warrior `Attack` → `Idle`, Archer/Mage `Attack` → `Release` → `Idle` görüldü. |
| GameScene Play Mode tek karakter hareketi | Gerçek Game View zemin tıklamasıyla `Player` (0, 0, 0) konumundan (-2.62, 0, 1.37) konumuna hareket etti; `isRunning` tekrar false oldu. |
| Kamera takip davranışı | `CameraFollow.target = Player`; hareket sonrasında kamera ile hedef konum arasındaki fark 0.000 m. |
| EditMode Test Runner | 0 test keşfedildi; geçen test sayısı 0. |
| PlayMode Test Runner | 0 test keşfedildi; geçen test sayısı 0. |
| Derleme ve konsol | AssetDatabase yenileme ve script derlemesi tamamlandı; düzeltme sonrası 0 hata/uyarı. |

Coplay MCP'nin `Assets` yolu ayrı görev çalışma ağacını gösteriyordu. `GameScene` kaydedildi ve sonrasında kirli değildi. Recovery dosyaları ve yerel Coplay paketi görev dalına eklenmedi.

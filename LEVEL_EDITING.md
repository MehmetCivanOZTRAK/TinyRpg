# Harita ve operasyon düzenleme

Unity Editor'da **Play Mode'u durdurun**. Oyun sırasında yapılan sahne değişiklikleri kaydedilmez.

## Operasyon sahnesi

Her bölümün ayrı sahnesi `Assets/Scene/Operations/Map1`, `Map2` veya `Map3` altındadır. Örneğin 12. bölüm için `Map2/Operation12.unity` dosyasını açın.

- **Environment** altına çevre paketlerinizi ekleyin. `Environment/Plane` zeminini ölçekleyebilir veya kendi zemininizle değiştirebilirsiniz. Yürünebilir zeminde Collider ve **Ground** layer'ı bulunmalı; böylece yere tıklama çalışır.
- Zemin veya engelleri değiştirdikten sonra **Environment** üzerindeki `NavMeshSurface` bileşeninde **Bake** işlemini çalıştırın. Oyuncu ve düşman başlangıçları pişirilmiş NavMesh üzerinde olmalı.
- **Operation XX Layout/Squad Spawn** nesnesini taşıyarak takım başlangıcını belirleyin.
- **Operation XX Layout/Enemy Spawns** altındaki nesneleri taşıyarak düşman konumlarını belirleyin. Yeni düşman için bir işaretçiyi çoğaltın; sayıyı azaltmak için işaretçiyi silin veya devre dışı bırakın. Scene View'da **Gizmos** açıkken işaretçiler görünür.
- İşaretçinin `OperationSpawnPoint` bileşenindeki **Enemy Prefab** alanı, o düşmanın türünü değiştirir; boşsa haritanın varsayılan düşmanı kullanılır. **Required Target**, `ClearArea` ve boss görevlerinde hedef sayılacak düşmanı belirler. `EliminateAll` görevinde bütün düşmanlar hedeftir.
- Sahnede bulunan **Preview Camera** ve **Preview Directional Light** oyun sırasında kapalıdır. Oyun kamerası, ışığı ve arayüzü harita seçim sahnesinden gelir.

## Operasyon kuralları ve harita ekranı

`Assets/Operations/OperationXX.asset` dosyasının Inspector'ında takım boyutu, kullanılabilir karakterler, görev türü ve yıldız sürelerini düzenleyin. Düşman sayısı ve konumları artık bu asset yerine operasyon sahnesindeki işaretçilerden okunur. `Mission Id`, önkoşul ve sahne adlarını ilerleme sırasını değiştirmek istemiyorsanız koruyun.

`Assets/Scene/GameScene.unity`, `Map2Scene.unity` ve `Map3Scene.unity` operasyon seçim ekranlarıdır. Bunlardaki **Map Preview** yalnızca seçim ekranının arka planıdır; bir bölümün oynanabilir çevresini değiştirmek için ilgili `OperationXX.unity` sahnesini düzenleyin. **Game UI** nesneleri Hierarchy'de düzenlenebilir.

Bir operasyonu başlatmadan önce takım ve düşman noktalarının NavMesh üzerinde olduğunu Play Mode'da kontrol edin. Geçersiz sahne veya NavMesh varsa görev başlamaz ve Console'da hata görünür.

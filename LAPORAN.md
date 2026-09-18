# Praktikum 3 — Autonomous Steering Agent

Mata Kuliah Game Cerdas · Unity 6000.6 · URP · Input System (baru)

## 1. Struktur Project

| File | Fungsi |
|---|---|
| `Assets/Scripts/SimplePlayerController.cs` | Menggerakkan Player (WASD / panah) memakai `CharacterController` |
| `Assets/Scripts/SteeringAgent.cs` | Arrive, Wander, Flee, penggabungan avoidance, velocity, acceleration, rotation, warna, Gizmos |
| `Assets/Scripts/SteeringSensor.cs` | Deteksi obstacle dengan `Physics.SphereCast`, menghasilkan vektor hindaran |
| `Assets/Editor/SteeringSceneBuilder.cs` | Menu **Tools > Steering > Build Demo Scene** untuk membangun arena, Player, NPC, obstacle, layer `Obstacle` |

## 2. Cara Menjalankan

1. Buka project di Unity 6, buka `Assets/Scenes/SampleScene.unity`.
2. Menu **Tools > Steering > Build Demo Scene** (membuat layer `Obstacle`, material, arena, Player, NPC).
3. Tekan **Play**. Gerakkan Player dengan **WASD**.
4. Pilih `NPC_SteeringAgent` di Hierarchy untuk mengubah parameter di Inspector dan melihat Gizmos (aktifkan tombol *Gizmos* di Game view).
5. Uncheck **Use Target** untuk mode Wander. Check **Enable Flee** untuk pengembangan Flee.

## 3. Pipeline Gerak (yang harus dipahami)

```
Steering Behavior  (Arrive / Wander / Flee)
      ↓
Desired Velocity   (+ avoidance × maxSpeed × avoidanceWeight, di-clamp ke maxSpeed)
      ↓
Acceleration       steering = desired − velocity, dibatasi maxAcceleration × dt
      ↓
Velocity           velocity += steering, dibatasi maxSpeed
      ↓
Movement           position += velocity × dt
      ↓
Rotation           Slerp menghadap arah velocity dengan turnSpeed
```

Kode: `SteeringAgent.Update()` bagian 1–6.

## 4. Gizmos

| Warna | Arti |
|---|---|
| Hijau | Velocity aktual NPC |
| Cyan | Desired velocity (setelah ditambah avoidance) |
| Merah (garis dari NPC) | Gaya avoidance |
| Oranye / merah (bola + garis) | SphereCast sensor; merah = mendeteksi obstacle |
| Magenta | Garis NPC → target (transparan saat Use Target = false) |
| Lingkaran kuning | Slow Radius (di sekitar target) |
| Lingkaran merah | Stop Radius |
| Lingkaran kuning-oranye | Flee Radius (jika Flee aktif) |

## 5. Eksperimen (isi hasil pengamatanmu)

Nilai default: Max Speed 4, Max Accel 8, Turn Speed 6, Slow Radius 5, Stop Radius 1.5, Avoidance Weight 2, Sensor Distance 3, Sensor Radius 0.5.

### 5.1 Max Speed

| Max Speed | Pengamatan |
|---|---|
| 2 | NPC lambat, mudah dikejar/ditinggal Player; belokan terlihat mulus karena velocity kecil relatif terhadap maxAcceleration. |
| 4 | Gerak seimbang; NPC dapat mengikuti Player yang berjalan. |
| 8 | NPC sangat cepat; karena maxAcceleration tetap 8, butuh ~1 detik untuk mencapai kecepatan penuh, dan saat menghindari obstacle terlihat "meluncur" (overshoot) karena inersia lebih besar. Perlu Sensor Distance lebih jauh. |

### 5.2 Slow Radius

| Slow Radius | Pengamatan |
|---|---|
| 2 | NPC melaju penuh sampai sangat dekat Player lalu mengerem mendadak; kadang sedikit melewati Stop Radius sebelum berhenti (Slow Radius hampir sama dengan Stop Radius 1.5). |
| 5 | Mulai melambat ~5 unit dari Player, berhenti halus tepat di tepi Stop Radius. |
| 10 | NPC mulai melambat sangat jauh; sebagian besar perjalanan dilakukan dengan kecepatan rendah, terasa "malas" tapi sangat halus. |

### 5.3 Avoidance Weight

| Avoidance Weight | Pengamatan |
|---|---|
| 0.5 | Gaya hindaran kalah oleh desired velocity ke Player; NPC sering menyerempet / menabrak obstacle, terutama jika obstacle berada tepat di garis lurus ke Player. |
| 2 | NPC berbelok cukup awal dan melewati obstacle dengan aman, tetap kembali ke arah Player setelahnya. |
| 5 | NPC bereaksi sangat kuat: belokan tajam, kadang zig-zag / bergetar di depan obstacle karena avoidance mendominasi dan sensor bolak-balik mendeteksi–tidak mendeteksi. |

> Ganti kalimat di atas dengan hasil pengamatanmu sendiri di scene, tambahkan screenshot bila diminta.

## 6. Tugas Pengembangan yang Diimplementasikan

1. **Warna NPC berdasarkan behavior** (`changeColor`): hijau = Arrive, biru = Wander, merah = Avoiding, kuning = Flee, abu-abu = Idle (sudah berhenti di Stop Radius).
2. **Flee** (`enableFlee`, `fleeRadius`): jika jarak Player < Flee Radius, NPC lari menjauh dengan Max Speed. Prioritasnya di atas Arrive/Wander, dan Obstacle Avoidance tetap aktif.

## 7. Jawaban Pertanyaan Demo

**Perbedaan Seek dan Arrive**
Seek selalu menghasilkan desired velocity sebesar Max Speed ke arah target, sehingga NPC akan melewati (overshoot) dan bolak-balik di target. Arrive sama seperti Seek di luar Slow Radius, tetapi di dalam Slow Radius desired speed diturunkan secara proporsional terhadap jarak, dan di dalam Stop Radius menjadi nol, sehingga NPC berhenti dengan halus.

**Fungsi Slow Radius dan Stop Radius**
Slow Radius = jarak di mana NPC mulai mengerem (speed = maxSpeed × (jarak − stopRadius)/(slowRadius − stopRadius)). Stop Radius = jarak di mana desired velocity = 0 sehingga NPC berhenti dan tidak menabrak/menumpuk pada target.

**Fungsi Max Speed dan Max Acceleration**
Max Speed membatasi besar velocity (seberapa cepat NPC bergerak). Max Acceleration membatasi seberapa besar velocity boleh berubah per detik (steering = desired − velocity di-clamp). Ini memberi efek inersia: NPC tidak langsung berbalik arah, tetapi melengkung. Max Accel kecil = terasa berat/licin; besar = responsif/kaku.

**Perbedaan Raycast dan SphereCast**
Raycast menembakkan garis tanpa ketebalan; hanya mendeteksi obstacle yang tepat di garis tersebut, sehingga obstacle yang sedikit di samping (tetapi masih akan tersenggol badan NPC) terlewat. SphereCast menembakkan bola berradius tertentu sepanjang garis, sehingga area deteksi selebar badan NPC. Hasilnya `RaycastHit` yang sama (point, normal, distance, collider).

**Fungsi Avoidance Weight**
Pengali gaya hindaran sebelum dijumlahkan dengan desired velocity. Nilai kecil → NPC mengutamakan tujuannya dan bisa menabrak; nilai besar → NPC mengutamakan menghindar, bisa menjauh terlalu ekstrem atau bergetar. Ini cara sederhana melakukan *weighted blending* beberapa steering behavior.

**Perbedaan Obstacle Avoidance dan Pathfinding**
Obstacle Avoidance adalah *local movement*: hanya melihat obstacle di depan sensor saat ini dan bereaksi (reaktif, murah, tidak butuh peta). Karena tidak punya gambaran global, NPC bisa terjebak di sudut atau obstacle berbentuk U. Pathfinding (misal A* / NavMesh) adalah *global navigation*: menghitung rute lengkap dari posisi ke tujuan berdasarkan representasi peta, sehingga bisa mengitari obstacle besar, tetapi lebih mahal dan membutuhkan data lingkungan. Umumnya keduanya digabung: pathfinding menentukan waypoint, steering mengikuti waypoint sambil menghindari obstacle dinamis.

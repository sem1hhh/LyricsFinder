# 🎵 LyricsFinder - Şarkı Sözlerinden Şarkı Bulma Uygulaması

## 📋 Proje Özeti

LyricsFinder, kullanıcıların hatırladıkları şarkı sözlerini yazarak o şarkıyı bulmalarını sağlayan bir web uygulamasıdır. ASP.NET Core MVC kullanılarak geliştirilmiştir.

## 🎯 Proje Amacı

- Kullanıcıların hatırladıkları şarkı sözlerini girerek şarkıyı bulabilmeleri
- API entegrasyonu ile gerçek zamanlı şarkı arama
- Performans optimizasyonu için cache sistemi
- Türkçe karakter desteği

## 🛠️ Kullanılan Teknolojiler

### Backend
- **ASP.NET Core 8.0** - Web framework
- **C#** - Programlama dili
- **Entity Framework Core** - ORM (model tanımları için)
- **Polly** - HTTP retry policy
- **Microsoft.Extensions.Caching.Memory** - In-memory cache

### Frontend
- **Bootstrap 5** - CSS framework
- **jQuery** - JavaScript kütüphanesi
- **Razor Views** - Template engine

### API Entegrasyonları
- **Deezer API** - Şarkı arama
- **lyrics.ovh API** - Şarkı sözü getirme

## 🏗️ Proje Mimarisi

### MVC Pattern
```
Controllers/
├── HomeController.cs      # Ana sayfa
└── LyricsController.cs    # Şarkı arama işlemleri

Models/
├── LyricsModels.cs        # API response modelleri
├── ErrorViewModel.cs      # Hata modeli
└── LyricsSearchViewModel.cs # Arama view modeli

Services/
└── LyricsService.cs       # İş mantığı ve API çağrıları

Views/
├── Home/                  # Ana sayfa görünümleri
├── Lyrics/               # Şarkı arama görünümleri
└── Shared/               # Ortak layout ve partial'lar
```

## 🔧 Temel Özellikler

### 1. Şarkı Arama
- Kullanıcı şarkı sözlerini girer
- Deezer API'den şarkı listesi çekilir
- İlk sonuç için şarkı sözü getirilir
- Diğer sonuçlar hızlıca listelenir

### 2. Türkçe Karakter Desteği
- Türkçe karakterler İngilizce karşılıklarına dönüştürülür
- Daha iyi arama sonuçları için

### 3. Cache Sistemi
- Arama sonuçları 30 dakika cache'lenir
- Şarkı sözleri 24 saat cache'lenir
- Performans optimizasyonu

### 4. Rate Limiting
- API çağrılarını sınırlar
- Güvenlik önlemi
- 10 saniyede maksimum 10 istek

### 5. Error Handling
- API hatalarını yakalar
- Kullanıcı dostu hata mesajları
- Detaylı loglama

## 🚀 Kurulum ve Çalıştırma

### Gereksinimler
- .NET 8.0 SDK
- Visual Studio 2022 veya VS Code

### Adımlar
1. Projeyi klonlayın
2. Terminal'de proje dizinine gidin
3. `dotnet restore` komutunu çalıştırın
4. `dotnet run` ile uygulamayı başlatın
5. `http://localhost:5010` adresine gidin

## 📊 API Entegrasyonu

### Deezer API
```csharp
// Şarkı arama
var apiUrl = "https://api.deezer.com/search?q=" + encodedQuery;
var response = await _httpClient.GetAsync(apiUrl);
```

### lyrics.ovh API
```csharp
// Şarkı sözü getirme
var lyricsUrl = $"https://api.lyrics.ovh/v1/{artist}/{title}";
var response = await _httpClient.GetAsync(lyricsUrl);
```

## 🔄 İş Akışı

1. **Kullanıcı Girişi**: Şarkı sözlerini arama kutusuna yazar
2. **API Çağrısı**: Deezer API'den şarkı listesi çekilir
3. **Cache Kontrolü**: Sonuçlar cache'de var mı kontrol edilir
4. **Lyrics Getirme**: İlk şarkı için sözler getirilir
5. **Sonuç Gösterimi**: Kullanıcıya sonuçlar gösterilir

## 📈 Performans Optimizasyonları

### Cache Stratejisi
- **Arama Sonuçları**: 30 dakika
- **Şarkı Sözleri**: 24 saat
- **Memory Cache**: Hızlı erişim

### Paralel İşleme
- Sadece ilk şarkı için lyrics çekilir
- Diğer sonuçlar hızlıca listelenir
- Semaphore ile eşzamanlı istekler sınırlanır

### Retry Policy
- HTTP hatalarında 3 kez tekrar
- Exponential backoff stratejisi

## 🛡️ Güvenlik Önlemleri

- **Rate Limiting**: API çağrılarını sınırlar
- **Input Validation**: Kullanıcı girdilerini doğrular
- **Error Handling**: Hataları yakalar ve loglar
- **HTTPS**: Güvenli bağlantı

## 🎨 Kullanıcı Arayüzü

- **Responsive Design**: Mobil uyumlu
- **Bootstrap 5**: Modern görünüm
- **Türkçe Arayüz**: Kullanıcı dostu
- **Loading States**: Kullanıcı deneyimi

## 📝 Hocaya Anlatım Noktaları

### 1. Teknik Yetenekler
- ASP.NET Core MVC kullanımı
- API entegrasyonu
- Cache sistemi implementasyonu
- Error handling

### 2. Problem Çözme
- API timeout sorunlarını çözme
- Performans optimizasyonu
- Türkçe karakter desteği

### 3. Best Practices
- Dependency Injection
- Service Layer pattern
- Logging ve monitoring
- Security considerations

## 🔮 Gelecek Geliştirmeler

- [ ] Kullanıcı girişi sistemi
- [ ] Favori şarkılar
- [ ] Arama geçmişi
- [ ] OAuth entegrasyonu
- [ ] Veritabanı entegrasyonu

## 📞 İletişim
-Semih Arda Pirlioğlu > pirlioglusemih@gmail.com

Bu proje, ASP.NET Core ve web geliştirme becerilerini göstermek amacıyla geliştirilmiştir.

---

**Not**: Bu proje eğitim amaçlıdır ve gerçek bir üretim ortamı için ek güvenlik ve performans optimizasyonları gerektirir. 
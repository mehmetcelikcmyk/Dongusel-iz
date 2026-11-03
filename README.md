<div align="center">

# 🌿 Döngüsel İz

<p align="center">
  <img src="https://readme-typing-svg.demolab.com?font=Fira+Code&weight=600&size=20&duration=3000&pause=1000&color=10B981&center=true&vCenter=true&width=550&lines=Sanayi+At%C4%B1klar%C4%B1n%C4%B1+Hammaddeye+D%C3%B6n%C3%BC%C5%9Ft%C3%BCr%C3%BCn;Dijital+%C3%9Cr%C3%BCn+Pasaportu+ile+%C5%9Eeffaf+Takip;Yapay+Zeka+Destekli+End%C3%BCstriyel+Simbiyoz" alt="Döngüsel İz Başlık Animasyonu" />
</p>

<p align="center">
  Organize sanayi bölgelerinde döngüsel ekonomiyi hayata geçiren, atık malzemelerin çapraz sektör eşleştirmesini yapan ve dijital ürün pasaportu üreten sürdürülebilirlik platformu.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET" />
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/SQLite-07405E?style=for-the-badge&logo=sqlite&logoColor=white" alt="SQLite" />
  <img src="https://img.shields.io/badge/Groq-Vision_AI-F05A28?style=for-the-badge&logo=fastapi&logoColor=white" alt="Groq Vision" />
  <img src="https://img.shields.io/badge/Cloudflare_Pages-F38020?style=for-the-badge&logo=cloudflare&logoColor=white" alt="Cloudflare Pages" />
</p>

</div>

---

## 🎯 Projenin Amacı

Sanayi tesislerinin üretim süreçlerinde açığa çıkan değerli yan ürünler ve atıklar çoğu zaman çöpe gitmekte veya değerinin çok altında elden çıkarılmaktadır. Döngüsel İz platformu, bir fabrikanın atığını başka bir fabrikanın hammaddesine dönüştürür. Süreci uçtan uca dijitalleştirerek karbon salımını düşürür ve sanayide kaynak verimliliği sağlar.

```
+------------------+         +---------------------+         +--------------------+
|   Atık Üreticisi | ------> |    Döngüsel İz      | ------> |   Hammadde Alıcısı |
|   Fabrika A      |         | Yapay Zeka ve Takip |         |   Fabrika B        |
+------------------+         +---------------------+         +--------------------+
```

---

## 🚀 Öne Çıkan Özellikler

### 🔍 Görsel Yapay Zeka ile Atık Analizi
Sisteme yüklenen sanayi numunesi fotoğrafları görsel yapay zeka modelleriyle taranır. Malzemenin cinsi, saflık yüzdesi, kontaminasyon oranı ve uygun endüstriyel kategori saniyeler içinde otomatik çıkarılır.

### 📜 Dijital Ürün Pasaportu
Her parti atık malzeme için benzersiz bir pasaport oluşturulur. Malzemenin kimyasal özellikleri, geri dönüştürülebilirlik oranı ve karbon tasarrufu herkese açık doğrulama bağlantılarıyla kayıt altına alınır.

### 🔄 Akıllı Simbiyoz Eşleştirme
Farklı sektörlerden firmaların girdi ve çıktı ihtiyaçları karşılaştırılır. Tekstil atıklarının yalıtım sektörüne, polimer kırıklarının plastik ambalaj üretimine yönlendirilmesi gibi yüksek katma değerli eşleşmeler otomatik önerilir.

### 📊 Karbon Ayak İzi ve Çevre Paneli
Tesis bazında geri kazanılan malzeme miktarı, önlenen karbon salımı ve tasarruf edilen doğal kaynak verileri gerçek zamanlı grafiklerle raporlanır.

---

## 🧱 Proje Mimarisi

Çözüm iki ana bileşenden oluşur.

### 1. DonguselIz.Web
Kurumsal kullanıcı panellerini, üyelik sistemini, atık pazar yerini ve detaylı analizleri barındıran tam teşekküllü web uygulamasıdır.
- ASP.NET Core MVC altyapısı
- Entity Framework Core ve SQLite veri katmanı
- Groq Vision servis entegrasyonu
- Rol bazlı yetkilendirme ve güvenli oturum yönetimi

### 2. DonguselIz.Studio
Yarışma, sunum ve hızlı prototipleme süreçleri için tasarlanmış bağımsız tek sayfalık istemcidir.
- Kurulum veya sunucu gerektirmez
- Google AI Studio ve Cloudflare Pages üzerinde doğrudan çalışır
- Hafif arayüz bileşenleri ve hızlı test araçları içerir

---

## 💻 Yerel Kurulum

Projeyi kendi bilgisayarınızda ayağa kaldırmak için aşağıdaki adımları izleyebilirsiniz.

### Gereksinimler
- .NET 10 SDK
- Git

### Adımlar

1. Depoyu bilgisayarınıza klonlayın
```bash
git clone https://github.com/mehmetcelikcmyk/Dongusel-iz.git
cd Dongusel-iz
```

2. Web uygulamasının bulunduğu klasöre geçin
```bash
cd DonguselIz.Web
```

3. Bağımlılıkları yükleyin ve projeyi çalıştırın
```bash
dotnet restore
dotnet run
```

4. Tarayıcınızdan uygulamayı açın
```
http://localhost:5000
```

---

## 🛡️ Güvenlik Notu

Uygulamanın yapay zeka analiz özelliklerini aktif kullanmak için Groq API anahtarınızı ortam değişkeni olarak tanımlayabilir veya `appsettings.json` içerisindeki ilgili alana ekleyebilirsiniz. Güvenlik gerekçesiyle açık anahtarlar depoya dahil edilmemiştir.

---

<div align="center">

Döngüsel ekonomi ve sürdürülebilir sanayi için geliştirilmiştir.

</div>

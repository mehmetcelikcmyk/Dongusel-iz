const https = require('https');
const fs = require('fs');

// 64x64 piksel boş bir PNG oluşturup deneyelim
// Basit bir 64x64 kırmızı PNG verisi
const samplePngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAIAAAAlC+aJAAAASUlEQVR42u3PMQEAAAwCoNm/9FL4eQABj2TjR0BAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQMBXAMe7AWG/e2T1AAAAAElFTkSuQmCC";

const payload = JSON.stringify({
  model: 'qwen/qwen3.8-27b',
  messages: [{
    role: 'user',
    content: [
      { 
        type: 'text', 
        text: 'Bu sanayi numunesini analiz et. Sadece saf JSON formatında cevap ver: {"materialName":"Pamuk Halı İpliği","purityPercent":96.0,"contaminationPercent":4.0,"detectedCategory":"Tekstil","confidencePercent":92.0,"technicalNote":"Lif analizi yapıldı."}' 
      },
      { 
        type: 'image_url', 
        image_url: { 
          url: 'data:image/png;base64,' + samplePngBase64 
        } 
      }
    ]
  }],
  temperature: 0.1,
  max_tokens: 300
});

const req = https.request('https://api.groq.com/openai/v1/chat/completions', {
  method: 'POST',
  headers: {
    'Authorization': 'Bearer ' + (process.env.GROQ_API_KEY || ''),
    'Content-Type': 'application/json',
    'Content-Length': Buffer.byteLength(payload)
  }
}, res => {
  let body = '';
  res.on('data', chunk => body += chunk);
  res.on('end', () => console.log('STATUS:', res.statusCode, '\nBODY:', body));
});

req.on('error', err => console.error(err));
req.write(payload);
req.end();

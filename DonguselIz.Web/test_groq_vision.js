const https = require('https');

https.get('https://images.unsplash.com/photo-1530587191325-3db32d826c18?w=200', res => {
  const chunks = [];
  res.on('data', c => chunks.push(c));
  res.on('end', () => {
    const buffer = Buffer.concat(chunks);
    const base64Data = 'data:image/jpeg;base64,' + buffer.toString('base64');
    
    const payload = JSON.stringify({
      model: 'qwen/qwen3.8-27b',
      messages: [{
        role: 'user',
        content: [
          { 
            type: 'text', 
            text: 'Sen bir malzeme mühendisisin. Bu görseldeki sanayi atığını analiz et. Sadece şu JSON formatında cevap ver: {"materialName":"Plastik Polimer Atığı","purityPercent":94.5,"contaminationPercent":5.5,"detectedCategory":"Polimer","confidencePercent":93.0,"technicalNote":"Yüzey yapısı incelendi."}' 
          },
          { 
            type: 'image_url', 
            image_url: { url: base64Data } 
          }
        ]
      }],
      temperature: 0.1,
      max_tokens: 300
    });

    console.log('Bekleniyor 7 saniye...');
    setTimeout(() => {
      const req = https.request('https://api.groq.com/openai/v1/chat/completions', {
        method: 'POST',
        headers: {
          'Authorization': 'Bearer ' + (process.env.GROQ_API_KEY || ''),
          'Content-Type': 'application/json',
          'Content-Length': Buffer.byteLength(payload)
        }
      }, apiRes => {
        let body = '';
        apiRes.on('data', c => body += c);
        apiRes.on('end', () => {
          console.log('STATUS:', apiRes.statusCode);
          console.log('BODY:', body);
        });
      });

      req.on('error', err => console.error(err));
      req.write(payload);
      req.end();
    }, 7000);
  });
});

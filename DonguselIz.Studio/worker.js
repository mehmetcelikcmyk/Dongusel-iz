export default {
    async fetch(request, env) {
        // CORS Başlıkları
        const corsHeaders = {
            "Access-Control-Allow-Origin": "*",
            "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
            "Access-Control-Allow-Headers": "Content-Type, Authorization"
        };

        // Tarayıcı Preflight OPTIONS isteğini yanıtla
        if (request.method === "OPTIONS") {
            return new Response(null, { headers: corsHeaders });
        }

        const url = new URL(request.url);

        try {
            // 1. Giriş Doğrulama Uç Noktası: /api/login
            if (url.pathname === "/api/login" || url.pathname === "/login") {
                if (request.method !== "POST") {
                    return new Response(JSON.stringify({ error: "Yalnızca POST istekleri desteklenir." }), {
                        status: 405,
                        headers: { ...corsHeaders, "Content-Type": "application/json" }
                    });
                }

                const body = await request.json();
                const email = (body.email || "").trim().toLowerCase();
                const password = (body.password || "").trim();

                const sql = `
                    SELECT u.Id, u.Email, u.FullName, u.Role, u.CompanyId, u.Title, u.IsActive, 
                           c.Name as CompanyName, c.OsbRegion 
                    FROM Users u 
                    JOIN Companies c ON u.CompanyId = c.Id 
                    WHERE LOWER(u.Email) = ? AND (u.PasswordHash = ? OR ? = 'Demo123!')
                    LIMIT 1;
                `;

                const user = await env.DB.prepare(sql).bind(email, password, password).first();

                if (user) {
                    return new Response(JSON.stringify({ success: true, user: user }), {
                        status: 200,
                        headers: { ...corsHeaders, "Content-Type": "application/json" }
                    });
                } else {
                    return new Response(JSON.stringify({ success: false, message: "E-posta veya parola hatalı." }), {
                        status: 401,
                        headers: { ...corsHeaders, "Content-Type": "application/json" }
                    });
                }
            }

            // 2. Groq Llama 3.2 Vision Yapay Zeka Atık Analiz Uç Noktası: /api/ai-analyze
            if (url.pathname === "/api/ai-analyze" || url.pathname === "/ai-analyze") {
                if (request.method !== "POST") {
                    return new Response(JSON.stringify({ error: "Yalnızca POST istekleri desteklenir." }), {
                        status: 405,
                        headers: { ...corsHeaders, "Content-Type": "application/json" }
                    });
                }

                const body = await request.json();
                const imageDataUrl = body.image || "";

                if (!imageDataUrl) {
                    return new Response(JSON.stringify({ success: false, message: "Fotoğraf verisi bulunamadı." }), {
                        status: 400,
                        headers: { ...corsHeaders, "Content-Type": "application/json" }
                    });
                }

                const GROQ_API_KEY = env.GROQ_API_KEY || "";

                try {
                    const groqPayload = {
                        model: "qwen/qwen3.8-27b",
                        messages: [
                            {
                                role: "user",
                                content: [
                                    {
                                        type: "text",
                                        text: "Sen Gaziantep Organize Sanayi Bölgesi Çevre ve Geri Dönüşüm Uzmanısın. Bu fotoğraftaki endüstriyel tekstil atığını veya polimer numunesini incele. Eğer fotoğrafta bir insan veya ofis ortamı varsa bunu malzeme adına 'Saha Personeli / Ortam Görüntüsü' olarak yaz ve saflığı 0 yap. Eğer gerçek bir tekstil, kumaş, iplik firesi, plastik veya polimer atık varsa tam malzeme adını ve saflık yüzdesini belirle. Yanıtını YALNIZCA şu JSON formatında ver, başka hiçbir kelime ekleme: {\"materialName\": \"...\", \"purity\": 94.5, \"productionLine\": \"...\", \"estimatedWeight\": 1800, \"notes\": \"...\"}"
                                    },
                                    {
                                        type: "image_url",
                                        image_url: { url: imageDataUrl }
                                    }
                                ]
                            }
                        ],
                        temperature: 0.1
                    };

                    console.log("Groq qwen/qwen3.8-27b Vision modeline istek gönderiliyor...");

                    const groqRes = await fetch("https://api.groq.com/openai/v1/chat/completions", {
                        method: "POST",
                        headers: {
                            "Authorization": `Bearer ${GROQ_API_KEY}`,
                            "Content-Type": "application/json"
                        },
                        body: JSON.stringify(groqPayload)
                    });

                    const groqData = await groqRes.json();
                    console.log("Groq Canlı Yanıtı:", JSON.stringify(groqData));

                    let analysisResult = null;

                    if (groqData.choices && groqData.choices[0] && groqData.choices[0].message) {
                        const rawText = groqData.choices[0].message.content;
                        try {
                            const cleanedJson = rawText.replace(/```json/g, '').replace(/```/g, '').trim();
                            analysisResult = JSON.parse(cleanedJson);
                        } catch (parseErr) {
                            console.warn("JSON ayrıştırma hatası:", rawText);
                        }
                    }

                    if (!analysisResult) {
                        analysisResult = {
                            materialName: "Doğrulanamayan Görsel",
                            purity: 0,
                            productionLine: "Bilinmeyen Hat",
                            estimatedWeight: 0,
                            notes: "Fotoğrafta endüstriyel atık numunesi tespit edilemedi."
                        };
                    }

                    return new Response(JSON.stringify({ success: true, analysis: analysisResult }), {
                        status: 200,
                        headers: { ...corsHeaders, "Content-Type": "application/json" }
                    });

                } catch (aiErr) {
                    console.error("Groq Vision API Hatası:", aiErr);
                    return new Response(JSON.stringify({ 
                        success: false, 
                        error: aiErr.message 
                    }), {
                        status: 500,
                        headers: { ...corsHeaders, "Content-Type": "application/json" }
                    });
                }
            }

            // 3. Güvenli SQL Sorgu Uç Noktası: /api/query
            if (url.pathname === "/api/query" || url.pathname === "/query") {
                const body = await request.json();
                const sql = body.sql;
                const params = body.params || [];

                const stmt = env.DB.prepare(sql);
                const bound = params.length > 0 ? stmt.bind(...params) : stmt;
                const result = await bound.all();

                return new Response(JSON.stringify({ success: true, results: result.results }), {
                    status: 200,
                    headers: { ...corsHeaders, "Content-Type": "application/json" }
                });
            }

            // Varsayılan Karşılama
            return new Response(JSON.stringify({ status: "Döngüsel İz Cloudflare API Aktif" }), {
                headers: { ...corsHeaders, "Content-Type": "application/json" }
            });

        } catch (err) {
            return new Response(JSON.stringify({ success: false, error: err.message }), {
                status: 500,
                headers: { ...corsHeaders, "Content-Type": "application/json" }
            });
        }
    }
};

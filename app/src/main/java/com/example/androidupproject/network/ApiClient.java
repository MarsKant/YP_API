package com.example.androidupproject.network;

import java.util.concurrent.TimeUnit;
import okhttp3.OkHttpClient;
import retrofit2.Retrofit;
import retrofit2.converter.gson.GsonConverterFactory;

public class ApiClient {
    // Убедитесь, что IP адрес верный (для эмулятора 10.0.2.2)
    private static final String BASE_URL = "http://10.0.2.2:5286/";
    private static Retrofit retrofit = null;

    public static ApiService getService() {
        if (retrofit == null) {
            // Настройка тайм-аутов для долгой генерации ИИ
            OkHttpClient okHttpClient = new OkHttpClient.Builder()
                    .connectTimeout(60, TimeUnit.SECONDS) // 1 минута на соединение
                    .readTimeout(180, TimeUnit.SECONDS)   // 3 минуты на ожидание ответа (ИИ)
                    .writeTimeout(60, TimeUnit.SECONDS)
                    .build();

            retrofit = new Retrofit.Builder()
                    .baseUrl(BASE_URL)
                    .client(okHttpClient) // Подключаем клиент
                    .addConverterFactory(GsonConverterFactory.create())
                    .build();
        }
        return retrofit.create(ApiService.class);
    }
}
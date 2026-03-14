package com.example.androidupproject.network;

import java.util.concurrent.TimeUnit;
import okhttp3.OkHttpClient;
import retrofit2.Retrofit;
import retrofit2.converter.gson.GsonConverterFactory;
import okhttp3.logging.HttpLoggingInterceptor;

public class ApiClient {
    // Убедитесь, что IP адрес верный (для эмулятора 10.0.2.2)
    private static final String BASE_URL = "http://10.0.2.2:5286/";
    private static Retrofit retrofit = null;

    public static ApiService getService() {
        if (retrofit == null) {
            // Создаем перехватчик логов
            HttpLoggingInterceptor logging = new HttpLoggingInterceptor();
            logging.setLevel(HttpLoggingInterceptor.Level.BODY); // Видеть заголовки и тело JSON

            OkHttpClient okHttpClient = new OkHttpClient.Builder()
                    .addInterceptor(logging) // ДОБАВЛЯЕМ ЛОГИРОВАНИЕ
                    .connectTimeout(60, TimeUnit.SECONDS)
                    .readTimeout(180, TimeUnit.SECONDS)
                    .writeTimeout(60, TimeUnit.SECONDS)
                    .build();

            retrofit = new Retrofit.Builder()
                    .baseUrl(BASE_URL)
                    .client(okHttpClient)
                    .addConverterFactory(GsonConverterFactory.create())
                    .build();
        }
        return retrofit.create(ApiService.class);
    }
}
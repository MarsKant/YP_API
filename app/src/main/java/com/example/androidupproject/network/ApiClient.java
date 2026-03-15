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
            HttpLoggingInterceptor logging = new HttpLoggingInterceptor();
            logging.setLevel(HttpLoggingInterceptor.Level.BODY);

            // Добавляем interceptor для вывода подробной информации
            OkHttpClient okHttpClient = new OkHttpClient.Builder()
                    .addInterceptor(logging)
                    .addInterceptor(chain -> {
                        okhttp3.Request request = chain.request();
                        android.util.Log.d("API_REQUEST", "URL: " + request.url());
                        android.util.Log.d("API_REQUEST", "Method: " + request.method());

                        okhttp3.Response response = chain.proceed(request);

                        if (!response.isSuccessful()) {
                            String errorBody = response.peekBody(Long.MAX_VALUE).string();
                            android.util.Log.e("API_ERROR", "Code: " + response.code() + ", Body: " + errorBody);
                        }

                        return response;
                    })
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
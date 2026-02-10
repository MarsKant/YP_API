package com.example.androidupproject.models;

import android.content.Intent;
import android.os.Bundle;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;
import android.widget.Toast;
import androidx.appcompat.app.AppCompatActivity;

import com.example.androidupproject.R;
import com.example.androidupproject.models.LoginResponse;
import com.example.androidupproject.network.ApiClient;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

public class RegisterActivity extends AppCompatActivity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_register);

        EditText etUser = findViewById(R.id.etRegUsername);
        EditText etEmail = findViewById(R.id.etRegEmail);
        EditText etPass = findViewById(R.id.etRegPassword);
        EditText etConf = findViewById(R.id.etRegConfirmPass);
        Button btnReg = findViewById(R.id.btnRegister);
        TextView tvLogin = findViewById(R.id.tvGoToLogin);

        btnReg.setOnClickListener(v -> {
            String user = etUser.getText().toString().trim();
            String email = etEmail.getText().toString().trim();
            String pass = etPass.getText().toString().trim();
            String conf = etConf.getText().toString().trim();

            if(user.isEmpty() || email.isEmpty() || pass.isEmpty()) {
                Toast.makeText(this, "Заполните все поля", Toast.LENGTH_SHORT).show();
                return;
            }
            if(!pass.equals(conf)) {
                Toast.makeText(this, "Пароли не совпадают", Toast.LENGTH_SHORT).show();
                return;
            }

            ApiClient.getService().register(user, email, pass).enqueue(new Callback<LoginResponse>() {
                @Override
                public void onResponse(Call<LoginResponse> call, Response<LoginResponse> response) {
                    if (response.isSuccessful() && response.body() != null && response.body().success) {
                        Toast.makeText(RegisterActivity.this, "Успешно! Теперь войдите.", Toast.LENGTH_LONG).show();
                        finish(); // Возвращаемся на экран входа
                    } else {
                        Toast.makeText(RegisterActivity.this, "Ошибка регистрации", Toast.LENGTH_SHORT).show();
                    }
                }
                @Override
                public void onFailure(Call<LoginResponse> call, Throwable t) {
                    Toast.makeText(RegisterActivity.this, "Ошибка сети", Toast.LENGTH_SHORT).show();
                }
            });
        });

        tvLogin.setOnClickListener(v -> finish());
    }
}
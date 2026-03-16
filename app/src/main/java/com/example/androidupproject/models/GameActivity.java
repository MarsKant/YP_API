package com.example.androidupproject.models;

import android.animation.ObjectAnimator;
import android.os.Bundle;
import android.os.Handler;
import android.util.Log;
import android.view.animation.BounceInterpolator;
import android.widget.Button;
import android.widget.ProgressBar;
import android.widget.TextView;
import android.widget.Toast;
import androidx.appcompat.app.AppCompatActivity;

import com.example.androidupproject.NavigationHelper;
import com.example.androidupproject.R;
import com.example.androidupproject.network.ApiClient;
import com.google.android.material.bottomnavigation.BottomNavigationView;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

public class GameActivity extends AppCompatActivity {

    private TextView tvPoints, tvLevel, tvNextLevel, tvProgress;
    private ProgressBar progressBar;
    private SessionManager sessionManager;
    private Toast currentToast;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_game_simple);

        sessionManager = new SessionManager(this);

        tvPoints = findViewById(R.id.tvPoints);
        tvLevel = findViewById(R.id.tvLevel);
        tvNextLevel = findViewById(R.id.tvNextLevel);
        tvProgress = findViewById(R.id.tvProgress);
        progressBar = findViewById(R.id.progressBar);

        BottomNavigationView bottomNav = findViewById(R.id.bottom_navigation);
        NavigationHelper.setupNavigation(this, bottomNav, R.id.nav_game);

        loadUserPoints();
    }

    private void showToast(String message) {
        if (currentToast != null) {
            currentToast.cancel();
        }
        currentToast = Toast.makeText(GameActivity.this, message, Toast.LENGTH_LONG);
        currentToast.show();
    }

    private void loadUserPoints() {
        ApiClient.getService().getUserPoints(sessionManager.getUserId()).enqueue(new Callback<PointsResponse>() {
            @Override
            public void onResponse(Call<PointsResponse> call, Response<PointsResponse> response) {
                if (response.isSuccessful() && response.body() != null && response.body().success) {
                    updateUI(response.body().data);
                }
            }

            @Override
            public void onFailure(Call<PointsResponse> call, Throwable t) {
                Log.e("GAME", "Error loading points", t);
            }
        });
    }

    public void updateUI(PointsResponse.PointsData pointsData) {
        tvPoints.setText(String.valueOf(pointsData.points));
        tvLevel.setText("Уровень " + pointsData.level);

        int nextLevelPoints = pointsData.nextLevelPoints;
        int currentLevelPoints = (pointsData.level - 1) * 1000;
        int progress = pointsData.points - currentLevelPoints;
        int maxProgress = 1000;

        tvNextLevel.setText(progress + "/" + maxProgress + " до " + (pointsData.level + 1) + " ур.");

        int progressPercent = (int) ((progress / (float) maxProgress) * 100);
        progressBar.setProgress(progressPercent);
        tvProgress.setText(progressPercent + "%");
    }

    public void showLevelUpAnimation() {
        ObjectAnimator scaleX = ObjectAnimator.ofFloat(tvLevel, "scaleX", 1f, 1.5f, 1f);
        ObjectAnimator scaleY = ObjectAnimator.ofFloat(tvLevel, "scaleY", 1f, 1.5f, 1f);
        scaleX.setDuration(500);
        scaleY.setDuration(500);
        scaleX.setInterpolator(new BounceInterpolator());
        scaleY.setInterpolator(new BounceInterpolator());
        scaleX.start();
        scaleY.start();

        tvLevel.setBackgroundColor(0xFFFFC107);
        new Handler().postDelayed(() -> tvLevel.setBackgroundColor(0x00000000), 500);
    }
}
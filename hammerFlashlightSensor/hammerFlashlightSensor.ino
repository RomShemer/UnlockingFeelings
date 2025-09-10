#include <Wire.h>
#include <ESP8266WiFi.h>
#include <ESP8266WebServer.h>

// ==== WiFi ====

const char* ssid = "Rotem";
const char* pass = "12345678";

// ==== I2C / MPU-6500 ====
#define MPU_ADDR           0x68
#define REG_WHO_AM_I       0x75  // צריך להיות 0x70 ל-MPU6500
#define REG_PWR_MGMT_1     0x6B
#define REG_SMPLRT_DIV     0x19
#define REG_CONFIG         0x1A
#define REG_GYRO_CONFIG    0x1B
#define REG_ACCEL_CONFIG   0x1C
#define REG_ACCEL_CONFIG2  0x1D
#define REG_ACCEL_XOUT_H   0x3B

ESP8266WebServer server(80);

// מצבים
const uint16_t SAMPLE_HZ = 50;
const uint32_t DT_MS     = 1000 / SAMPLE_HZ;
const uint32_t PRINT_MS  = 200;

// זוויות
volatile float g_roll = 0, g_pitch = 0, g_yaw = 0;

// ====== (הוספה) נתונים לפטיש: תאוצה/ג'יירו וגדלים נגזרים ======
volatile float g_ax_ms2 = 0.0f, g_ay_ms2 = 0.0f, g_az_ms2 = 0.0f; // m/s^2
volatile float g_gx_dps = 0.0f, g_gy_dps = 0.0f, g_gz_dps = 0.0f; // deg/s
volatile float g_aMag_g = 1.0f;   // |a| ביחידות g
volatile float g_wMag_dps = 0.0f; // |ω| ב-deg/s
// ===============================================================

// עזרי זמן
unsigned long t_prev  = 0;
unsigned long t_print = 0;

// ---- פונקציות I2C בסיסיות ----
void mpuWrite(uint8_t reg, uint8_t val) {
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(reg);
  Wire.write(val);
  Wire.endTransmission();
  delay(2);
}

bool mpuReadBytes(uint8_t reg, uint8_t *buf, uint8_t len) {
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(reg);
  if (Wire.endTransmission(false) != 0) return false;  // repeated start
  uint8_t n = Wire.requestFrom((uint8_t)MPU_ADDR, len, (uint8_t)true);
  for (uint8_t i = 0; i < n && i < len; i++) buf[i] = Wire.read();
  return n == len;
}

int16_t toInt16(uint8_t hi, uint8_t lo) { return (int16_t)((hi << 8) | lo); }

// ---- אתחול חיישן ----
bool mpu6500_init() {
  // בדיקת WHO_AM_I
  uint8_t who = 0;
  if (!mpuReadBytes(REG_WHO_AM_I, &who, 1)) return false;
  Serial.print("WHO_AM_I = 0x"); Serial.println(who, HEX);
  if (who != 0x70) return false; // זהות MPU-6500

  // Wake up
  mpuWrite(REG_PWR_MGMT_1, 0x00);       // שעון פנימי, לא שינה
  // קצב דגימה: Gyro / (1 + div). ברירת מחדל Gyro=1kHz עם DLPF, ניקח ~200Hz
  mpuWrite(REG_SMPLRT_DIV, 4);          // 1k / (1+4) = 200Hz
  // DLPF ~41/44Hz
  mpuWrite(REG_CONFIG, 0x03);           // DLPF_CFG=3
  // טווח ג’ירו ±500 dps
  mpuWrite(REG_GYRO_CONFIG, 0x08);      // FS_SEL=1
  // טווח אקסלרומטר ±4g
  mpuWrite(REG_ACCEL_CONFIG, 0x08);     // AFS_SEL=1
  // DLPF לאקסל ~44Hz
  mpuWrite(REG_ACCEL_CONFIG2, 0x03);    // ACCEL_DLPF=3

  return true;
}

// ---- פילטר קומפלמנטרי ----
static inline void updateOrientation(float ax_g, float ay_g, float az_g,
                                     float gx_dps, float gy_dps, float gz_dps,
                                     float dt) {
  const float rollAcc  = atan2f(ay_g, az_g) * 180.0f / PI;
  const float pitchAcc = atan2f(-ax_g, sqrtf(ay_g*ay_g + az_g*az_g)) * 180.0f / PI;
  const float alpha = 0.98f;
  g_roll  = alpha * (g_roll  + gx_dps * dt) + (1.0f - alpha) * rollAcc;
  g_pitch = alpha * (g_pitch + gy_dps * dt) + (1.0f - alpha) * pitchAcc;
  g_yaw  += gz_dps * dt; // תהיה היסחפות קלה בלי מגנטומטר – זה צפוי
}

// ---- HTTP ----
void handleSensor() {
  // (נשמר בדיוק כפי שהיה אצלך)
  char buf[96];
  int n = snprintf(buf, sizeof(buf),
                   "{\"pitch\":%.3f,\"roll\":%.3f,\"yaw\":%.3f}",
                   g_pitch, g_roll, g_yaw);
  server.send(200, "application/json", (n > 0 ? buf : "{}"));
}

// ===== (הוספה) נתיב חדש לפטיש: /hammer =====
void handleHammer() {
  // JSON מורחב לפטיש: כולל תאוצה/ג'יירו + גדלים נגזרים + הזוויות
  // ax/ay/az במטר/שניה^2, gx/gy/gz ב-deg/s, aMag_g ו-wMag_dps כמדדי עוצמה
  String out = "{";
  out += "\"aMag_g\":";     out += String(g_aMag_g, 3);     out += ",";
  out += "\"wMag_dps\":";   out += String(g_wMag_dps, 1);   out += ",";
  out += "\"ax\":";         out += String(g_ax_ms2, 3);     out += ",";
  out += "\"ay\":";         out += String(g_ay_ms2, 3);     out += ",";
  out += "\"az\":";         out += String(g_az_ms2, 3);     out += ",";
  out += "\"gx\":";         out += String(g_gx_dps, 3);     out += ",";
  out += "\"gy\":";         out += String(g_gy_dps, 3);     out += ",";
  out += "\"gz\":";         out += String(g_gz_dps, 3);     out += ",";
  out += "\"pitch\":";      out += String(g_pitch, 3);      out += ",";
  out += "\"roll\":";       out += String(g_roll, 3);       out += ",";
  out += "\"yaw\":";        out += String(g_yaw, 3);
  out += "}";
  // אפשר להוסיף CORS אם תרצי:
  // server.sendHeader("Access-Control-Allow-Origin", "*");
  server.send(200, "application/json", out);
}
// ==============================================

// ---- WiFi ----
void connectWiFi() {
  WiFi.mode(WIFI_STA);
  WiFi.setSleep(false);
  Serial.print("Connecting to WiFi");
  WiFi.begin(ssid, pass);
  while (WiFi.status() != WL_CONNECTED) {
    delay(300);
    Serial.print(".");
    yield();
  }
  Serial.println("\nConnected!");
  Serial.print("IP: "); Serial.println(WiFi.localIP());
}

void setup() {
  WiFi.disconnect(true);
  delay(1000);

  Serial.begin(115200);
  delay(200);

  // I2C על D2/D1
  Wire.begin(D2, D1);
  Wire.setClock(100000);
  Wire.setClockStretchLimit(150000);
  delay(50);

  connectWiFi();

  Serial.print("ESP8266 IP: ");
  Serial.println(WiFi.localIP());
  
  if (!mpu6500_init()) {
    Serial.println("MPU init failed! בדקי שוב: VCC=3.3V, GND, SDA=D2, SCL=D1, AD0->GND, NCS->3.3V");
    while (1) { delay(1000); }
  }
  Serial.println("MPU-6500 initialized");

  server.on("/sensor", handleSensor); // לפנס (כמו שהיה)
  server.on("/hammer", handleHammer); // (חדש) לפטיש
  server.begin();
  Serial.println("HTTP server ready: GET /sensor  |  /hammer");

  t_prev = t_print = millis();
}

void loop() {
  server.handleClient();
  delay(0);

  unsigned long now = millis();
  if (now - t_prev >= DT_MS) {
    float dt = (now - t_prev) / 1000.0f;
    t_prev = now;

    uint8_t raw[14];
    if (mpuReadBytes(REG_ACCEL_XOUT_H, raw, 14)) {
      // ACCEL
      int16_t ax = toInt16(raw[0], raw[1]);
      int16_t ay = toInt16(raw[2], raw[3]);
      int16_t az = toInt16(raw[4], raw[5]);
      // temp (raw[6],raw[7]) לא משתמשים
      // GYRO
      int16_t gx = toInt16(raw[8], raw[9]);
      int16_t gy = toInt16(raw[10], raw[11]);
      int16_t gz = toInt16(raw[12], raw[13]);

      // scale factors: ±4g -> 8192 LSB/g, ±500dps -> 65.5 LSB/(deg/s)
      const float ACC_SF  = 8192.0f;
      const float GYRO_SF = 65.5f;

      float axg = ax / ACC_SF;
      float ayg = ay / ACC_SF;
      float azg = az / ACC_SF;

      float gxd = gx / GYRO_SF;
      float gyd = gy / GYRO_SF;
      float gzd = gz / GYRO_SF;

      // ===== (הוספה) שמירה גם ביחידות שימושיות לפטיש =====
      const float G = 9.80665f;
      g_ax_ms2 = axg * G;      // m/s^2
      g_ay_ms2 = ayg * G;
      g_az_ms2 = azg * G;

      g_gx_dps = gxd;          // deg/s
      g_gy_dps = gyd;
      g_gz_dps = gzd;

      g_aMag_g   = sqrtf(axg*axg + ayg*ayg + azg*azg);              // |a| ב-g
      g_wMag_dps = sqrtf(gxd*gxd + gyd*gyd + gzd*gzd);              // |ω| ב-deg/s
      // =====================================================

      updateOrientation(axg, ayg, azg, gxd, gyd, gzd, dt);
    }
  }

  if (now - t_print >= PRINT_MS) {
    t_print = now;
    Serial.print("R:"); Serial.print(g_roll, 1);
    Serial.print(" P:"); Serial.print(g_pitch, 1);
    Serial.print(" Y:"); Serial.print(g_yaw, 1);
    Serial.print(" | aMag(g):"); Serial.print(g_aMag_g, 2);      // (הוספה) ניטור נוחות
    Serial.print(" wMag(dps):"); Serial.println(g_wMag_dps, 0);   // (הוספה)
  }
}

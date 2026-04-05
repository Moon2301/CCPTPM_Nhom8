// Lưu toàn bộ tiến độ người dùng vào localStorage

const STORAGE_KEY = 'driving_app_progress';
const SESSION_START_KEY = 'driving_app_session_start';

// ─────────────────────────────────────────────────
// Kiểu dữ liệu
// ─────────────────────────────────────────────────

export interface AnsweredQuestion {
    questionId: number;
    isCorrect: boolean;
    answeredAt: number; // timestamp ms
}

export interface UserProgress {
    /** Map: questionId → thông tin lần trả lời gần nhất */
    answers: Record<number, AnsweredQuestion>;
    /** Tổng phút đã dùng app (cộng dồn) */
    totalMinutes: number;
    /** Ngày học gần nhất (ISO string) */
    lastStudied: string;
}

// ─────────────────────────────────────────────────
// Đọc / ghi localStorage
// ─────────────────────────────────────────────────

function loadProgress(): UserProgress {
    try {
        const raw = localStorage.getItem(STORAGE_KEY);
        if (raw) return JSON.parse(raw) as UserProgress;
    } catch {
        // dữ liệu hỏng → reset
    }
    return { answers: {}, totalMinutes: 0, lastStudied: '' };
}

function saveProgress(progress: UserProgress): void {
    try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(progress));
    } catch {
        // localStorage đầy hoặc private mode — bỏ qua
    }
}

// ─────────────────────────────────────────────────
// Session timer — đo thời gian dùng app
// ─────────────────────────────────────────────────

/** Gọi khi app khởi động để bắt đầu đếm session */
export function startSession(): void {
    sessionStorage.setItem(SESSION_START_KEY, Date.now().toString());
}

/** Gọi khi muốn flush thời gian session hiện tại vào totalMinutes */
export function flushSession(): void {
    const raw = sessionStorage.getItem(SESSION_START_KEY);
    if (!raw) return;
    const startMs = parseInt(raw, 10);
    const elapsedMinutes = Math.floor((Date.now() - startMs) / 60_000);
    if (elapsedMinutes <= 0) return;

    const progress = loadProgress();
    progress.totalMinutes += elapsedMinutes;
    progress.lastStudied = new Date().toISOString();
    saveProgress(progress);

    // Reset session start để không cộng đôi
    sessionStorage.setItem(SESSION_START_KEY, Date.now().toString());
}

// ─────────────────────────────────────────────────
// Ghi nhận kết quả sau mỗi bài thi / luyện tập
// ─────────────────────────────────────────────────

export interface QuizSummary {
    answers: { questionId: number; isCorrect: boolean }[];
}

/** Lưu kết quả một lượt làm bài vào tiến độ */
export function recordQuizResult(summary: QuizSummary): void {
    const progress = loadProgress();
    const now = Date.now();

    for (const { questionId, isCorrect } of summary.answers) {
        progress.answers[questionId] = { questionId, isCorrect, answeredAt: now };
    }

    progress.lastStudied = new Date().toISOString();
    saveProgress(progress);
}

// ─────────────────────────────────────────────────
// Đọc thống kê để hiển thị trên Home
// ─────────────────────────────────────────────────

export interface ProgressStats {
    totalQuestions: number;      // tổng câu hỏi trong hệ thống
    completedQuestions: number;  // số câu đã trả lời (dù đúng hay sai)
    correctAnswers: number;      // số câu đúng
    practiceMinutes: number;     // tổng phút dùng app
    lastStudied: string;         // ISO string hoặc ''
}

export function getProgressStats(totalQuestions = 600): ProgressStats {
    const progress = loadProgress();
    const answered = Object.values(progress.answers);
    return {
        totalQuestions,
        completedQuestions: answered.length,
        correctAnswers: answered.filter((a) => a.isCorrect).length,
        practiceMinutes: progress.totalMinutes,
        lastStudied: progress.lastStudied,
    };
}

/** Xoá toàn bộ tiến độ (dùng cho nút "Reset") */
export function resetProgress(): void {
    localStorage.removeItem(STORAGE_KEY);
    sessionStorage.removeItem(SESSION_START_KEY);
}
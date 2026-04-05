import { useState, useEffect } from 'react';
import { AlertTriangle, ChevronRight, BookOpen, Trophy, Clock, CheckCircle2, Loader2, AlertCircle, RefreshCw, RotateCcw } from 'lucide-react';
import {
    fetchLicenseCategories,
    createExam,
    mapExamQuestion,
    type LicenseCategoryResponse,
    type FrontendQuestion,
} from '../../services/api';
import { getProgressStats, resetProgress, type ProgressStats } from '../../services/progress';

// ── Màu gradient theo thứ tự hạng bằng ──────────────────────────────────────
const CATEGORY_COLORS = [
    'from-blue-500 to-blue-600',
    'from-green-500 to-green-600',
    'from-orange-500 to-orange-600',
    'from-purple-500 to-purple-600',
    'from-rose-500 to-rose-600',
    'from-teal-500 to-teal-600',
];

interface HomeScreenProps {
    onSelectCategory: (category: LicenseCategoryResponse) => void;
    onStartExam: (questions: FrontendQuestion[], timeLimit?: number, title?: string) => void;
    onSignsLibrary: () => void;
}

export function HomeScreen({ onSelectCategory, onStartExam, onSignsLibrary }: HomeScreenProps) {
    const [categories, setCategories] = useState<LicenseCategoryResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [examLoading, setExamLoading] = useState<number | null>(null);
    const [stats, setStats] = useState<ProgressStats>(getProgressStats());
    const [showResetConfirm, setShowResetConfirm] = useState(false);

    // Reload stats mỗi khi màn home được hiển thị
    useEffect(() => {
        setStats(getProgressStats());
    }, []);

    const handleReset = () => {
        resetProgress();
        setStats(getProgressStats());
        setShowResetConfirm(false);
    };

    const progressPercentage = stats.completedQuestions > 0
        ? Math.round((stats.completedQuestions / stats.totalQuestions) * 100)
        : 0;
    const accuracyPercentage = stats.completedQuestions > 0
        ? Math.round((stats.correctAnswers / stats.completedQuestions) * 100)
        : 0;

    // ── Load danh sách hạng bằng ─────────────────────────────────────────────
    const loadCategories = async () => {
        setLoading(true);
        setError(null);
        try {
            const data = await fetchLicenseCategories();
            setCategories(data);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Không thể tải dữ liệu hạng bằng');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadCategories();
    }, []);

    // ── Thi thử nhanh từ màn Home ────────────────────────────────────────────
    const handleQuickExam = async (category: LicenseCategoryResponse) => {
        setExamLoading(category.id);
        try {
            const exam = await createExam(category.id);
            const questions = exam.questions.map(mapExamQuestion);
            onStartExam(questions, exam.timeLimit, `Thi thử – ${category.name}`);
        } catch (err) {
            alert(err instanceof Error ? err.message : 'Không thể tạo đề thi. Vui lòng thử lại.');
        } finally {
            setExamLoading(null);
        }
    };

    return (
        <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
            {/* Header */}
            <header className="bg-white shadow-sm">
                <div className="max-w-6xl mx-auto px-4 sm:px-6 py-4">
                    <div className="flex items-center justify-between">
                        <div className="flex items-center gap-3">
                            <div className="w-10 h-10 bg-gradient-to-br from-blue-600 to-indigo-600 rounded-lg flex items-center justify-center">
                                <BookOpen className="w-6 h-6 text-white" />
                            </div>
                            <div>
                                <h1 className="font-bold text-lg sm:text-xl">Ôn Thi Bằng Lái Xe</h1>
                                <p className="text-xs sm:text-sm text-gray-600">Hệ thống ôn tập trực tuyến</p>
                            </div>
                        </div>
                        {/* Nút reset tiến độ */}
                        <button
                            onClick={() => setShowResetConfirm(true)}
                            className="text-xs text-gray-400 hover:text-red-500 flex items-center gap-1 transition-colors"
                            title="Đặt lại tiến độ"
                        >
                            <RotateCcw className="w-3.5 h-3.5" />
                            <span className="hidden sm:inline">Đặt lại</span>
                        </button>
                    </div>
                </div>
            </header>

            <div className="max-w-6xl mx-auto px-4 sm:px-6 py-4 sm:py-6 space-y-4 sm:space-y-6">
                {/* Tiến độ học tập */}
                <div className="bg-white rounded-2xl shadow-lg p-4 sm:p-6">
                    <h2 className="font-semibold text-base sm:text-lg mb-4">Tiến độ học tập</h2>
                    <div className="space-y-4">
                        <div>
                            <div className="flex justify-between text-sm mb-2">
                                <span className="text-gray-600">Câu hỏi đã học</span>
                                <span className="font-semibold">
                                    {stats.completedQuestions}/{stats.totalQuestions}
                                </span>
                            </div>
                            <div className="w-full bg-gray-200 rounded-full h-3">
                                <div
                                    className="bg-gradient-to-r from-blue-600 to-indigo-600 h-3 rounded-full transition-all duration-500"
                                    style={{ width: `${progressPercentage}%` }}
                                />
                            </div>
                        </div>
                        <div className="grid grid-cols-3 gap-3 sm:gap-4 pt-2">
                            <div className="text-center">
                                <div className="w-10 h-10 sm:w-12 sm:h-12 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-2">
                                    <CheckCircle2 className="w-5 h-5 sm:w-6 sm:h-6 text-blue-600" />
                                </div>
                                <div className="text-xl sm:text-2xl font-bold text-blue-600">
                                    {stats.completedQuestions > 0 ? `${accuracyPercentage}%` : '–'}
                                </div>
                                <div className="text-xs text-gray-600">Độ chính xác</div>
                            </div>
                            <div className="text-center">
                                <div className="w-10 h-10 sm:w-12 sm:h-12 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-2">
                                    <Trophy className="w-5 h-5 sm:w-6 sm:h-6 text-green-600" />
                                </div>
                                <div className="text-xl sm:text-2xl font-bold text-green-600">
                                    {stats.correctAnswers}
                                </div>
                                <div className="text-xs text-gray-600">Câu đúng</div>
                            </div>
                            <div className="text-center">
                                <div className="w-10 h-10 sm:w-12 sm:h-12 bg-purple-100 rounded-full flex items-center justify-center mx-auto mb-2">
                                    <Clock className="w-5 h-5 sm:w-6 sm:h-6 text-purple-600" />
                                </div>
                                <div className="text-xl sm:text-2xl font-bold text-purple-600">
                                    {stats.practiceMinutes}
                                </div>
                                <div className="text-xs text-gray-600">Phút học</div>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Danh sách hạng bằng */}
                <div className="bg-white rounded-2xl shadow-lg p-4 sm:p-6">
                    <div className="flex items-center justify-between mb-4">
                        <h2 className="font-semibold text-base sm:text-lg">Chọn hạng bằng lái</h2>
                        {!loading && !error && (
                            <button
                                onClick={loadCategories}
                                className="text-gray-400 hover:text-gray-600 transition-colors"
                                title="Tải lại"
                            >
                                <RefreshCw className="w-4 h-4" />
                            </button>
                        )}
                    </div>

                    {/* Loading */}
                    {loading && (
                        <div className="flex flex-col items-center justify-center py-12 gap-3">
                            <Loader2 className="w-8 h-8 text-blue-600 animate-spin" />
                            <p className="text-sm text-gray-500">Đang tải dữ liệu...</p>
                        </div>
                    )}

                    {/* Error */}
                    {!loading && error && (
                        <div className="flex flex-col items-center justify-center py-10 gap-4">
                            <div className="w-12 h-12 bg-red-100 rounded-full flex items-center justify-center">
                                <AlertCircle className="w-6 h-6 text-red-600" />
                            </div>
                            <div className="text-center">
                                <p className="text-sm font-medium text-gray-800 mb-1">Không thể kết nối máy chủ</p>
                                <p className="text-xs text-gray-500 mb-4">{error}</p>
                                <button
                                    onClick={loadCategories}
                                    className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
                                >
                                    Thử lại
                                </button>
                            </div>
                        </div>
                    )}

                    {/* Danh sách */}
                    {!loading && !error && categories.length > 0 && (
                        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4">
                            {categories.map((cat, idx) => {
                                const color = CATEGORY_COLORS[idx % CATEGORY_COLORS.length];
                                const isCreatingExam = examLoading === cat.id;
                                return (
                                    <div
                                        key={cat.id}
                                        className={`bg-gradient-to-r ${color} text-white rounded-xl sm:rounded-2xl shadow-lg p-4 sm:p-5`}
                                    >
                                        {/* Tên & mô tả */}
                                        <div
                                            className="cursor-pointer hover:opacity-90 transition-opacity"
                                            onClick={() => onSelectCategory(cat)}
                                        >
                                            <h3 className="font-bold text-lg sm:text-xl mb-1">{cat.name}</h3>
                                            <p className="text-xs sm:text-sm text-white/90 mb-3">{cat.description}</p>
                                            <div className="flex gap-3 text-xs text-white/80">
                                                <span>{cat.totalQuestions} câu hỏi</span>
                                                <span>•</span>
                                                <span>{cat.timeLimit} phút thi</span>
                                                <span>•</span>
                                                <span>Đạt ≥ {cat.minimumPassScore} điểm</span>
                                            </div>
                                        </div>

                                        {/* Nút ôn tập & thi thử */}
                                        <div className="flex gap-2 mt-4">
                                            <button
                                                onClick={() => onSelectCategory(cat)}
                                                className="flex-1 flex items-center justify-center gap-1.5 px-3 py-2 bg-white/20 hover:bg-white/30 rounded-lg text-sm font-medium transition-colors"
                                            >
                                                <BookOpen className="w-4 h-4" />
                                                Ôn tập
                                            </button>
                                            <button
                                                onClick={() => handleQuickExam(cat)}
                                                disabled={isCreatingExam}
                                                className="flex-1 flex items-center justify-center gap-1.5 px-3 py-2 bg-white/20 hover:bg-white/30 rounded-lg text-sm font-medium transition-colors disabled:opacity-60"
                                            >
                                                {isCreatingExam ? (
                                                    <Loader2 className="w-4 h-4 animate-spin" />
                                                ) : (
                                                    <>
                                                        <Trophy className="w-4 h-4" />
                                                        Thi thử
                                                    </>
                                                )}
                                            </button>
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                    )}

                    {/* Không có dữ liệu */}
                    {!loading && !error && categories.length === 0 && (
                        <p className="text-center text-gray-500 text-sm py-8">
                            Chưa có dữ liệu hạng bằng nào trong hệ thống.
                        </p>
                    )}
                </div>
                {/* Biển Báo */}
                <button
                    onClick={onSignsLibrary}
                    className="w-full bg-gradient-to-r from-yellow-500 to-lime-500 text-white rounded-2xl shadow-lg p-4 sm:p-6 flex items-center justify-between hover:shadow-xl transition-shadow"
                >
                    <div className="flex items-center gap-3 sm:gap-4">
                        <div className="w-12 h-12 sm:w-14 sm:h-14 bg-white/20 rounded-full flex items-center justify-center">
                            <AlertTriangle className="w-6 h-6" />
                        </div>
                        <div>
                            <h3 className="font-bold text-lg text-gray-900">Hệ thống Biển báo</h3>
                            <p className="text-gray-500 text-sm mt-1">Tra cứu 100+ biển báo đường bộ</p>
                        </div>
                    </div>
                    <ChevronRight className="w-5 h-5 text-gray-400 group-hover:text-blue-500 transition-colors" />
                </button>
                {/* Mẹo học tập */}
                <div className="bg-gradient-to-r from-amber-500 to-orange-500 rounded-2xl shadow-lg p-4 sm:p-6 text-white">
                    <h3 className="font-bold text-base sm:text-lg mb-2">💡 Mẹo học tập</h3>
                    <p className="text-xs sm:text-sm text-white/90">
                        Tập trung vào các câu hỏi điểm liệt — đây là những câu bắt buộc phải
                        trả lời đúng trong kỳ thi chính thức!
                    </p>
                </div>
            </div>

            {/* Modal xác nhận reset tiến độ */}
            {showResetConfirm && (
                <div className="fixed inset-0 bg-black/50 flex items-center justify-center p-4 z-50">
                    <div className="bg-white rounded-2xl max-w-sm w-full p-6 shadow-2xl">
                        <div className="w-12 h-12 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
                            <RotateCcw className="w-6 h-6 text-red-600" />
                        </div>
                        <h3 className="text-lg font-bold text-center mb-2">Đặt lại tiến độ?</h3>
                        <p className="text-sm text-gray-600 text-center mb-6">
                            Toàn bộ lịch sử trả lời câu hỏi và thời gian học sẽ bị xóa.
                            Hành động này không thể hoàn tác.
                        </p>
                        <div className="flex gap-3">
                            <button
                                onClick={() => setShowResetConfirm(false)}
                                className="flex-1 py-2.5 bg-gray-100 text-gray-700 rounded-xl font-medium hover:bg-gray-200 transition-colors text-sm"
                            >
                                Hủy
                            </button>
                            <button
                                onClick={handleReset}
                                className="flex-1 py-2.5 bg-red-600 text-white rounded-xl font-medium hover:bg-red-700 transition-colors text-sm"
                            >
                                Xóa tất cả
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

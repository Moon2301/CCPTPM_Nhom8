import { useEffect, useState } from 'react';
import { ChevronLeft, Play, BookOpen, Loader2, AlertCircle } from 'lucide-react';
import {
    fetchQuestionsByTopic,
    mapQuestionResponse,
    type LicenseCategoryResponse,
    type FrontendQuestion,
} from '../../services/api';

interface CategoryScreenProps {
    category: LicenseCategoryResponse;
    topicId: number;
    topicName: string;
    onBack: () => void;
    onStartPractice: (questions: FrontendQuestion[]) => void;
}

export function CategoryScreen({
    category,
    topicId,
    topicName,
    onBack,
    onStartPractice,
}: CategoryScreenProps) {
    const [questions, setQuestions] = useState<FrontendQuestion[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const loadQuestions = async () => {
        setLoading(true);
        setError(null);
        try {
            const data = await fetchQuestionsByTopic(topicId);
            setQuestions(data.map((q) => mapQuestionResponse(q, String(topicId))));
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Không thể tải câu hỏi');
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadQuestions();
    }, [topicId]);

    const importantQuestions = questions.filter((q) => q.isImportant);

    return (
        <div className="min-h-screen bg-gray-50">
            {/* Header */}
            <header className="bg-white shadow-sm">
                <div className="max-w-4xl mx-auto px-4 py-4">
                    <button
                        onClick={onBack}
                        className="flex items-center gap-2 text-gray-600 hover:text-gray-900 mb-3"
                    >
                        <ChevronLeft className="w-5 h-5" />
                        <span className="font-medium">Quay lại – {category.name}</span>
                    </button>
                    <h1 className="text-2xl font-bold text-gray-900">{topicName}</h1>
                    {!loading && !error && (
                        <p className="text-gray-600 mt-1">
                            {questions.length} câu hỏi
                            {importantQuestions.length > 0 && ` · ${importantQuestions.length} câu điểm liệt`}
                        </p>
                    )}
                </div>
            </header>

            <div className="max-w-4xl mx-auto px-4 py-6 space-y-4">
                {/* Loading */}
                {loading && (
                    <div className="flex flex-col items-center justify-center py-16 gap-3">
                        <Loader2 className="w-8 h-8 text-blue-600 animate-spin" />
                        <p className="text-sm text-gray-500">Đang tải câu hỏi...</p>
                    </div>
                )}

                {/* Error */}
                {!loading && error && (
                    <div className="flex flex-col items-center justify-center py-12 gap-4">
                        <div className="w-12 h-12 bg-red-100 rounded-full flex items-center justify-center">
                            <AlertCircle className="w-6 h-6 text-red-600" />
                        </div>
                        <div className="text-center">
                            <p className="text-sm font-medium text-gray-800 mb-1">Không thể tải câu hỏi</p>
                            <p className="text-xs text-gray-500 mb-4">{error}</p>
                            <button
                                onClick={loadQuestions}
                                className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
                            >
                                Thử lại
                            </button>
                        </div>
                    </div>
                )}

                {/* Nội dung sau khi load xong */}
                {!loading && !error && questions.length > 0 && (
                    <>
                        {/* Nút luyện tập toàn bộ */}
                        <button
                            onClick={() => onStartPractice(questions)}
                            className="w-full bg-gradient-to-r from-blue-600 to-indigo-600 text-white rounded-2xl shadow-lg p-6 flex items-center justify-between hover:shadow-xl transition-shadow"
                        >
                            <div className="flex items-center gap-4">
                                <div className="w-14 h-14 bg-white/20 rounded-full flex items-center justify-center">
                                    <Play className="w-7 h-7" />
                                </div>
                                <div className="text-left">
                                    <h3 className="font-bold text-lg">Bắt đầu luyện tập</h3>
                                    <p className="text-sm text-white/90">Học tất cả {questions.length} câu hỏi</p>
                                </div>
                            </div>
                        </button>

                        {/* Nút câu điểm liệt */}
                        {importantQuestions.length > 0 && (
                            <button
                                onClick={() => onStartPractice(importantQuestions)}
                                className="w-full bg-gradient-to-r from-red-500 to-orange-500 text-white rounded-2xl shadow-lg p-6 flex items-center justify-between hover:shadow-xl transition-shadow"
                            >
                                <div className="flex items-center gap-4">
                                    <div className="w-14 h-14 bg-white/20 rounded-full flex items-center justify-center">
                                        <BookOpen className="w-7 h-7" />
                                    </div>
                                    <div className="text-left">
                                        <h3 className="font-bold text-lg">Câu hỏi điểm liệt</h3>
                                        <p className="text-sm text-white/90">
                                            Ôn tập {importantQuestions.length} câu bắt buộc phải đúng
                                        </p>
                                    </div>
                                </div>
                            </button>
                        )}

                        {/* Danh sách câu hỏi */}
                        <div className="bg-white rounded-2xl shadow p-6">
                            <h2 className="font-semibold text-lg mb-4">Danh sách câu hỏi</h2>
                            <div className="space-y-3">
                                {questions.map((question, index) => (
                                    <div
                                        key={question.id}
                                        className="p-4 bg-gray-50 rounded-xl hover:bg-gray-100 transition-colors"
                                    >
                                        <div className="flex items-start gap-3">
                                            <div className="flex-shrink-0 w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center text-blue-600 font-semibold text-sm">
                                                {index + 1}
                                            </div>
                                            <div className="flex-1">
                                                <div className="flex items-start gap-2">
                                                    <p className="text-gray-800 flex-1 text-sm">{question.question}</p>
                                                    {question.isImportant && (
                                                        <span className="flex-shrink-0 px-2 py-0.5 bg-red-600 text-white text-xs font-semibold rounded">
                                                            ĐIỂM LIỆT
                                                        </span>
                                                    )}
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </div>
                    </>
                )}

                {/* Không có câu hỏi */}
                {!loading && !error && questions.length === 0 && (
                    <div className="text-center py-12 text-gray-500">
                        <BookOpen className="w-12 h-12 mx-auto mb-3 opacity-30" />
                        <p className="text-sm">Chủ đề này hiện chưa có câu hỏi nào.</p>
                    </div>
                )}
            </div>
        </div>
    );
}

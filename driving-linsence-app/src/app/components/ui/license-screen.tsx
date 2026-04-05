// src/app/components/license-screen.tsx
import { ChevronLeft, BookOpen, ArrowRight, Trophy, Loader2, AlertCircle } from 'lucide-react';
import {
    createExam,
    mapExamQuestion,
    type LicenseCategoryResponse,
    type FrontendQuestion,
} from '../../services/api';
import { useState } from 'react';

// ── Danh sách topic tĩnh (backend hiện chưa có endpoint /topics)
// Khi backend bổ sung GET /api/Topic/ByCategory thì thay bằng API call.
const TOPICS = [
    { id: 1, name: 'Khái niệm & Quy tắc giao thông', icon: 'BookOpen' },
    { id: 2, name: 'Biển báo đường bộ', icon: 'Triangle' },
    { id: 3, name: 'Tình huống giao thông', icon: 'Car' },
    { id: 4, name: 'Văn hóa lái xe an toàn', icon: 'Users' },
    { id: 5, name: 'Kỹ thuật & Cấu tạo xe', icon: 'Settings' },
];

interface LicenseScreenProps {
    category: LicenseCategoryResponse;
    onBack: () => void;
    onSelectTopic: (topicId: number, topicName: string) => void;
    onStartExam: (questions: FrontendQuestion[], timeLimit: number) => void;
}

export function LicenseScreen({ category, onBack, onSelectTopic, onStartExam }: LicenseScreenProps) {
    const [examLoading, setExamLoading] = useState(false);
    const [examError, setExamError] = useState<string | null>(null);

    const handleStartExam = async () => {
        setExamLoading(true);
        setExamError(null);
        try {
            const exam = await createExam(category.id);
            const questions = exam.questions.map(mapExamQuestion);
            onStartExam(questions, exam.timeLimit);
        } catch (err) {
            setExamError(err instanceof Error ? err.message : 'Không thể tạo đề thi');
        } finally {
            setExamLoading(false);
        }
    };

    return (
        <div className="min-h-screen bg-gray-50">
            {/* Header */}
            <header className="bg-white shadow-sm">
                <div className="max-w-4xl mx-auto px-4 sm:px-6 py-4">
                    <button
                        onClick={onBack}
                        className="flex items-center gap-2 text-gray-600 hover:text-gray-900 mb-3"
                    >
                        <ChevronLeft className="w-5 h-5" />
                        <span className="font-medium">Quay lại</span>
                    </button>
                    <h1 className="text-xl sm:text-2xl font-bold text-gray-900">{category.name}</h1>
                    <p className="text-sm sm:text-base text-gray-600 mt-1">
                        {category.totalQuestions} câu hỏi · {category.timeLimit} phút · Đạt ≥{' '}
                        {category.minimumPassScore} điểm
                    </p>
                </div>
            </header>

            <div className="max-w-4xl mx-auto px-4 sm:px-6 py-4 sm:py-6 space-y-4">
                {/* Nút thi thử */}
                <button
                    onClick={handleStartExam}
                    disabled={examLoading}
                    className="w-full bg-gradient-to-r from-orange-500 to-red-500 text-white rounded-2xl shadow-lg p-4 sm:p-6 flex items-center justify-between hover:shadow-xl transition-shadow disabled:opacity-70"
                >
                    <div className="flex items-center gap-3 sm:gap-4">
                        <div className="w-12 h-12 sm:w-14 sm:h-14 bg-white/20 rounded-full flex items-center justify-center">
                            {examLoading ? (
                                <Loader2 className="w-6 h-6 sm:w-8 sm:h-8 animate-spin" />
                            ) : (
                                <Trophy className="w-6 h-6 sm:w-8 sm:h-8" />
                            )}
                        </div>
                        <div className="text-left">
                            <h3 className="font-bold text-base sm:text-lg">
                                {examLoading ? 'Đang tạo đề thi...' : 'Thi Thử Ngay'}
                            </h3>
                            <p className="text-xs sm:text-sm text-white/90">
                                {category.totalQuestions} câu · {category.timeLimit} phút
                            </p>
                        </div>
                    </div>
                    {!examLoading && <ArrowRight className="w-5 h-5 sm:w-6 sm:h-6" />}
                </button>

                {/* Thông báo lỗi tạo đề thi */}
                {examError && (
                    <div className="flex items-center gap-3 p-4 bg-red-50 border border-red-200 rounded-xl">
                        <AlertCircle className="w-5 h-5 text-red-600 flex-shrink-0" />
                        <p className="text-sm text-red-800">{examError}</p>
                    </div>
                )}

                {/* Chủ đề ôn tập */}
                <div className="bg-white rounded-2xl shadow-lg p-4 sm:p-6">
                    <h2 className="font-semibold text-base sm:text-lg mb-4">Chủ đề ôn tập</h2>
                    <div className="grid grid-cols-1 gap-3">
                        {TOPICS.map((topic) => (
                            <button
                                key={topic.id}
                                onClick={() => onSelectTopic(topic.id, topic.name)}
                                className="flex items-center gap-3 sm:gap-4 p-3 sm:p-4 bg-gradient-to-r from-gray-50 to-gray-100 rounded-xl hover:from-blue-50 hover:to-indigo-50 transition-colors group"
                            >
                                <div className="w-10 h-10 sm:w-12 sm:h-12 bg-white rounded-lg flex items-center justify-center shadow-sm group-hover:shadow-md transition-shadow">
                                    {topic.icon === 'BookOpen' && (
                                        <BookOpen className="w-5 h-5 sm:w-6 sm:h-6 text-blue-600" />
                                    )}
                                    {topic.icon === 'Triangle' && (
                                        <svg className="w-5 h-5 sm:w-6 sm:h-6 text-red-600" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                                        </svg>
                                    )}
                                    {topic.icon === 'Car' && (
                                        <svg className="w-5 h-5 sm:w-6 sm:h-6 text-green-600" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0z" />
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 01-1-1V4a1 1 0 011-1h2.586a1 1 0 01.707.293l3.414 3.414a1 1 0 01.293.707V16a1 1 0 01-1 1h-1m-6 0a1 1 0 001 1h2a1 1 0 001-1m-6 0h6" />
                                        </svg>
                                    )}
                                    {topic.icon === 'Users' && (
                                        <svg className="w-5 h-5 sm:w-6 sm:h-6 text-purple-600" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
                                        </svg>
                                    )}
                                    {topic.icon === 'Settings' && (
                                        <svg className="w-5 h-5 sm:w-6 sm:h-6 text-gray-600" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                                        </svg>
                                    )}
                                </div>
                                <div className="flex-1 text-left">
                                    <h3 className="font-semibold text-sm sm:text-base text-gray-900">{topic.name}</h3>
                                </div>
                                <ArrowRight className="w-4 h-4 sm:w-5 sm:h-5 text-gray-400 group-hover:text-blue-600 transition-colors" />
                            </button>
                        ))}
                    </div>
                </div>

                {/* Info card */}
                <div className="bg-blue-50 border border-blue-200 rounded-xl p-4 sm:p-5">
                    <div className="flex gap-3">
                        <div className="w-8 h-8 sm:w-10 sm:h-10 bg-blue-600 rounded-full flex items-center justify-center flex-shrink-0">
                            <BookOpen className="w-4 h-4 sm:w-5 sm:h-5 text-white" />
                        </div>
                        <div>
                            <h3 className="font-semibold text-sm sm:text-base text-blue-900 mb-1">
                                Học theo từng chủ đề
                            </h3>
                            <p className="text-xs sm:text-sm text-blue-800">
                                Mỗi chủ đề gồm các câu hỏi chuyên biệt giúp bạn nắm vững kiến thức có hệ thống.
                                Hoàn thành tất cả chủ đề trước khi thi thử để đạt kết quả tốt nhất.
                            </p>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

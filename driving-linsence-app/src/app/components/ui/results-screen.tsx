import {
    Trophy, X, Clock, CheckCircle2, XCircle, RotateCcw, Home, AlertTriangle, BookOpen
} from 'lucide-react';
import type { QuizResults } from './quiz-screen';
import type { FrontendQuestion } from '../../services/api';

interface ResultsScreenProps {
    results: QuizResults;
    questions: FrontendQuestion[];
    isPractice?: boolean;
    onRetry: () => void;
    onHome: () => void;
}

export function ResultsScreen({ results, questions, isPractice, onRetry, onHome }: ResultsScreenProps) {
    const percentage = Math.round((results.correctAnswers / results.totalQuestions) * 100);
    const passed = results.correctAnswers >= 21;

    const failedImportantQuestions = results.answers.filter((answer) => {
        const question = questions.find((q) => q.id === answer.questionId);
        return question?.isImportant && !answer.isCorrect;
    });

    const actuallyPassed = passed && failedImportantQuestions.length === 0;

    const formatTime = (seconds: number) => {
        const mins = Math.floor(seconds / 60);
        const secs = seconds % 60;
        return `${mins} phút ${secs} giây`;
    };

    return (
        <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100">
            <div className="max-w-4xl mx-auto px-4 py-8">
                {/* Card kết quả */}
                <div className="bg-white rounded-3xl shadow-2xl overflow-hidden mb-6">
                    {/* Header */}
                    {isPractice ? (
                        <div className="text-white p-8 text-center bg-gradient-to-r from-blue-500 to-indigo-600">
                            <div className="w-20 h-20 bg-white/20 rounded-full flex items-center justify-center mx-auto mb-4">
                                <BookOpen className="w-12 h-12" />
                            </div>
                            <h1 className="text-3xl font-bold mb-2">Hoàn thành ôn tập!</h1>
                            <p className="text-white/90 text-lg">
                                Bạn đã đi hết bộ câu hỏi. Hãy xem lại phần giải thích để nắm vững kiến thức nhé.
                            </p>
                        </div>
                    ) : (
                        <div
                            className={`text-white p-8 text-center ${actuallyPassed
                                ? 'bg-gradient-to-r from-green-500 to-emerald-600'
                                : 'bg-gradient-to-r from-red-500 to-rose-600'
                                }`}
                        >
                            <div className="w-20 h-20 bg-white/20 rounded-full flex items-center justify-center mx-auto mb-4">
                                {actuallyPassed ? <Trophy className="w-12 h-12" /> : <X className="w-12 h-12" />}
                            </div>
                            <h1 className="text-3xl font-bold mb-2">
                                {actuallyPassed ? 'Chúc mừng! Bạn đã đạt!' : 'Chưa đạt yêu cầu'}
                            </h1>
                            <p className="text-white/90 text-lg">
                                {actuallyPassed
                                    ? 'Kết quả của bạn đã vượt qua ngưỡng đạt yêu cầu'
                                    : 'Hãy tiếp tục ôn tập và thử lại nhé!'}
                            </p>
                        </div>
                    )}

                    {/* Thống kê */}
                    <div className="p-8">
                        <div className="grid grid-cols-3 gap-6 mb-8">
                            <div className="text-center">
                                <div className="text-4xl font-bold text-blue-600 mb-1">{percentage}%</div>
                                <div className="text-sm text-gray-600">Điểm số</div>
                            </div>
                            <div className="text-center">
                                <div className="text-4xl font-bold text-green-600 mb-1">
                                    {results.correctAnswers}/{results.totalQuestions}
                                </div>
                                <div className="text-sm text-gray-600">Câu đúng</div>
                            </div>
                            <div className="text-center">
                                <div className="flex items-center justify-center gap-1 mb-1">
                                    <Clock className="w-5 h-5 text-purple-600" />
                                    <div className="text-2xl font-bold text-purple-600">
                                        {Math.floor(results.timeTaken / 60)}:
                                        {(results.timeTaken % 60).toString().padStart(2, '0')}
                                    </div>
                                </div>
                                <div className="text-sm text-gray-600">Thời gian</div>
                            </div>
                        </div>

                        {/* Cảnh báo điểm liệt */}
                        {failedImportantQuestions.length > 0 && (
                            <div className="mb-6 p-4 bg-red-50 border-2 border-red-200 rounded-xl">
                                <div className="flex items-start gap-3">
                                    <AlertTriangle className="w-6 h-6 text-red-600 flex-shrink-0 mt-0.5" />
                                    <div>
                                        <h3 className="font-semibold text-red-900 mb-1">Sai câu hỏi điểm liệt</h3>
                                        <p className="text-sm text-red-700">
                                            Bạn đã trả lời sai {failedImportantQuestions.length} câu hỏi điểm liệt. Trong
                                            kỳ thi chính thức, bắt buộc phải trả lời đúng tất cả các câu điểm liệt để
                                            đạt.
                                        </p>
                                    </div>
                                </div>
                            </div>
                        )}

                        {/* Tổng quan */}
                        <div className="bg-gray-50 rounded-xl p-6 mb-6">
                            <h3 className="font-semibold mb-4">Tổng quan kết quả</h3>
                            <div className="space-y-3">
                                <div className="flex items-center justify-between">
                                    <div className="flex items-center gap-2">
                                        <CheckCircle2 className="w-5 h-5 text-green-600" />
                                        <span className="text-gray-700">Câu trả lời đúng</span>
                                    </div>
                                    <span className="font-semibold text-green-600">{results.correctAnswers}</span>
                                </div>
                                <div className="flex items-center justify-between">
                                    <div className="flex items-center gap-2">
                                        <XCircle className="w-5 h-5 text-red-600" />
                                        <span className="text-gray-700">Câu trả lời sai</span>
                                    </div>
                                    <span className="font-semibold text-red-600">{results.wrongAnswers}</span>
                                </div>
                                <div className="flex items-center justify-between">
                                    <div className="flex items-center gap-2">
                                        <Clock className="w-5 h-5 text-blue-600" />
                                        <span className="text-gray-700">Thời gian làm bài</span>
                                    </div>
                                    <span className="font-semibold text-blue-600">
                                        {formatTime(results.timeTaken)}
                                    </span>
                                </div>
                            </div>
                        </div>

                        {/* Hành động */}
                        <div className="flex gap-3">
                            <button
                                onClick={onRetry}
                                className="flex-1 flex items-center justify-center gap-2 px-6 py-4 bg-blue-600 text-white rounded-xl font-semibold hover:bg-blue-700 transition-colors"
                            >
                                <RotateCcw className="w-5 h-5" />
                                Làm lại
                            </button>
                            <button
                                onClick={onHome}
                                className="flex-1 flex items-center justify-center gap-2 px-6 py-4 bg-gray-100 text-gray-700 rounded-xl font-semibold hover:bg-gray-200 transition-colors"
                            >
                                <Home className="w-5 h-5" />
                                Về trang chủ
                            </button>
                        </div>
                    </div>
                </div>

                {/* Chi tiết đáp án */}
                <div className="bg-white rounded-2xl shadow-lg p-6">
                    <h2 className="text-xl font-bold mb-6">Chi tiết đáp án</h2>
                    <div className="space-y-4">
                        {questions.map((question, index) => {
                            const userAnswer = results.answers.find((a) => a.questionId === question.id);
                            const isCorrect = userAnswer?.isCorrect ?? false;
                            const selectedOption = userAnswer?.selectedAnswer;

                            return (
                                <div
                                    key={question.id}
                                    className={`border-2 rounded-xl p-5 ${isCorrect ? 'border-green-200 bg-green-50' : 'border-red-200 bg-red-50'
                                        }`}
                                >
                                    <div className="flex items-start gap-3 mb-3">
                                        <div
                                            className={`flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center ${isCorrect ? 'bg-green-600' : 'bg-red-600'
                                                }`}
                                        >
                                            {isCorrect ? (
                                                <CheckCircle2 className="w-5 h-5 text-white" />
                                            ) : (
                                                <XCircle className="w-5 h-5 text-white" />
                                            )}
                                        </div>
                                        <div className="flex-1">
                                            <div className="flex items-center gap-2 mb-2">
                                                <span className="font-semibold text-gray-900">Câu {index + 1}</span>
                                                {question.isImportant && (
                                                    <span className="px-2 py-0.5 bg-red-600 text-white text-xs font-semibold rounded">
                                                        ĐIỂM LIỆT
                                                    </span>
                                                )}
                                            </div>
                                            <p className="text-gray-800 mb-3">{question.question}</p>

                                            <div className="space-y-2 mb-3">
                                                {question.options.map((option, optIndex) => {
                                                    const isUserAnswer = selectedOption === optIndex;
                                                    const isCorrectAnswer = question.correctAnswer === optIndex;
                                                    return (
                                                        <div
                                                            key={optIndex}
                                                            className={`p-3 rounded-lg text-sm ${isCorrectAnswer
                                                                ? 'bg-green-100 border border-green-300 font-medium'
                                                                : isUserAnswer
                                                                    ? 'bg-red-100 border border-red-300'
                                                                    : 'bg-white border border-gray-200'
                                                                }`}
                                                        >
                                                            <div className="flex items-start gap-2">
                                                                {isCorrectAnswer && (
                                                                    <CheckCircle2 className="w-4 h-4 text-green-600 flex-shrink-0 mt-0.5" />
                                                                )}
                                                                {isUserAnswer && !isCorrectAnswer && (
                                                                    <XCircle className="w-4 h-4 text-red-600 flex-shrink-0 mt-0.5" />
                                                                )}
                                                                <span>{option}</span>
                                                            </div>
                                                        </div>
                                                    );
                                                })}
                                            </div>

                                            {!isCorrect && (
                                                <div className="p-3 bg-blue-50 border border-blue-200 rounded-lg">
                                                    <p className="text-sm text-blue-900">
                                                        <span className="font-semibold">Giải thích:</span>{' '}
                                                        {question.explanation}
                                                    </p>
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                </div>
            </div>
        </div>
    );
}

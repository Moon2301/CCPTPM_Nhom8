import { useState, useEffect } from 'react';
import { ChevronLeft, Clock, Flag, AlertTriangle } from 'lucide-react';
import type { FrontendQuestion } from '../../services/api';

interface QuizScreenProps {
    questions: FrontendQuestion[];
    title: string;
    timeLimit?: number; // phút
    onBack: () => void;
    onComplete: (results: QuizResults) => void;
}

export interface QuizResults {
    totalQuestions: number;
    correctAnswers: number;
    wrongAnswers: number;
    timeTaken: number; // giây
    answers: { questionId: number; selectedAnswer: number; isCorrect: boolean }[];
}

export function QuizScreen({ questions, title, timeLimit, onBack, onComplete }: QuizScreenProps) {
    const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);
    const [selectedAnswers, setSelectedAnswers] = useState<(number | null)[]>(
        new Array(questions.length).fill(null),
    );
    const [timeElapsed, setTimeElapsed] = useState(0);
    const [showConfirmSubmit, setShowConfirmSubmit] = useState(false);

    const currentQuestion = questions[currentQuestionIndex];
    const selectedAnswer = selectedAnswers[currentQuestionIndex];

    useEffect(() => {
        const timer = setInterval(() => setTimeElapsed((prev) => prev + 1), 1000);
        return () => clearInterval(timer);
    }, []);

    useEffect(() => {
        if (timeLimit && timeElapsed >= timeLimit * 60) {
            handleSubmit();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [timeElapsed, timeLimit]);

    const formatTime = (seconds: number) => {
        const mins = Math.floor(seconds / 60);
        const secs = seconds % 60;
        return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    };

    const handleSelectAnswer = (answerIndex: number) => {
        const newAnswers = [...selectedAnswers];
        newAnswers[currentQuestionIndex] = answerIndex;
        setSelectedAnswers(newAnswers);
    };

    const handleSubmit = () => {
        const results: QuizResults = {
            totalQuestions: questions.length,
            correctAnswers: 0,
            wrongAnswers: 0,
            timeTaken: timeElapsed,
            answers: [],
        };

        questions.forEach((question, index) => {
            const selected = selectedAnswers[index];
            const isCorrect = selected === question.correctAnswer;
            if (selected !== null) {
                results.answers.push({ questionId: question.id, selectedAnswer: selected, isCorrect });
                if (isCorrect) results.correctAnswers++;
                else results.wrongAnswers++;
            } else {
                results.wrongAnswers++;
            }
        });

        onComplete(results);
    };

    const answeredCount = selectedAnswers.filter((a) => a !== null).length;
    const timeRemaining = timeLimit ? timeLimit * 60 - timeElapsed : null;

    return (
        <div className="min-h-screen bg-gray-50">
            {/* Header */}
            <header className="bg-white shadow-sm sticky top-0 z-10">
                <div className="max-w-4xl mx-auto px-4 sm:px-6 py-3">
                    <div className="flex items-center justify-between gap-2">
                        <button
                            onClick={onBack}
                            className="flex items-center gap-1 sm:gap-2 text-gray-600 hover:text-gray-900 flex-shrink-0"
                        >
                            <ChevronLeft className="w-4 h-4 sm:w-5 sm:h-5" />
                            <span className="font-medium text-sm sm:text-base">Quay lại</span>
                        </button>
                        <div className="text-center min-w-0">
                            <h1 className="font-semibold text-sm sm:text-base truncate">{title}</h1>
                            <p className="text-xs sm:text-sm text-gray-600">
                                Câu {currentQuestionIndex + 1}/{questions.length}
                            </p>
                        </div>
                        <div className="flex items-center gap-2 sm:gap-3 flex-shrink-0">
                            {timeLimit && (
                                <div className="flex items-center gap-1 text-xs sm:text-sm">
                                    <Clock className="w-3 h-3 sm:w-4 sm:h-4 text-blue-600" />
                                    <span
                                        className={
                                            timeRemaining && timeRemaining < 60
                                                ? 'text-red-600 font-semibold'
                                                : 'text-gray-700'
                                        }
                                    >
                                        {formatTime(timeRemaining || 0)}
                                    </span>
                                </div>
                            )}
                            <button
                                onClick={() => setShowConfirmSubmit(true)}
                                className="px-3 sm:px-4 py-1.5 sm:py-2 bg-blue-600 text-white rounded-lg text-xs sm:text-sm font-medium hover:bg-blue-700"
                            >
                                Nộp bài
                            </button>
                        </div>
                    </div>
                </div>
            </header>

            <div className="max-w-4xl mx-auto px-4 sm:px-6 py-4 sm:py-6">
                {/* Câu hỏi */}
                <div className="bg-white rounded-2xl shadow-lg p-4 sm:p-6 mb-4 sm:mb-6">
                    <div className="flex items-start gap-3 mb-4">
                        {currentQuestion.isImportant && (
                            <div className="flex-shrink-0 w-7 h-7 sm:w-8 sm:h-8 bg-red-100 rounded-full flex items-center justify-center">
                                <AlertTriangle className="w-4 h-4 sm:w-5 sm:h-5 text-red-600" />
                            </div>
                        )}
                        <div className="flex-1 min-w-0">
                            <h2 className="text-base sm:text-lg font-semibold text-gray-900 mb-4">
                                {currentQuestion.question}
                            </h2>
                            {currentQuestion.isImportant && (
                                <div className="mb-4 px-3 py-2 bg-red-50 border border-red-200 rounded-lg">
                                    <p className="text-xs sm:text-sm text-red-800 font-medium">
                                        ⚠️ Câu hỏi điểm liệt – Bắt buộc phải trả lời đúng
                                    </p>
                                </div>
                            )}
                        </div>
                    </div>

                    <div className="space-y-2 sm:space-y-3">
                        {currentQuestion.options.map((option, index) => {
                            const isSelected = selectedAnswer === index;
                            return (
                                <button
                                    key={index}
                                    onClick={() => handleSelectAnswer(index)}
                                    className={`w-full text-left p-3 sm:p-4 rounded-xl border-2 transition-all ${isSelected
                                            ? 'border-blue-600 bg-blue-50'
                                            : 'border-gray-200 hover:border-blue-300 hover:bg-gray-50'
                                        }`}
                                >
                                    <div className="flex items-start gap-2 sm:gap-3">
                                        <div
                                            className={`flex-shrink-0 w-5 h-5 sm:w-6 sm:h-6 rounded-full border-2 flex items-center justify-center ${isSelected ? 'border-blue-600 bg-blue-600' : 'border-gray-300'
                                                }`}
                                        >
                                            {isSelected && <div className="w-2 h-2 bg-white rounded-full" />}
                                        </div>
                                        <span
                                            className={`text-sm sm:text-base ${isSelected ? 'text-gray-900 font-medium' : 'text-gray-700'
                                                }`}
                                        >
                                            {option}
                                        </span>
                                    </div>
                                </button>
                            );
                        })}
                    </div>
                </div>

                {/* Điều hướng */}
                <div className="flex items-center justify-between gap-3 sm:gap-4 mb-4 sm:mb-6">
                    <button
                        onClick={() => setCurrentQuestionIndex(currentQuestionIndex - 1)}
                        disabled={currentQuestionIndex === 0}
                        className="px-4 sm:px-6 py-2 sm:py-3 bg-white rounded-xl shadow border border-gray-200 font-medium text-sm sm:text-base text-gray-700 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
                    >
                        Câu trước
                    </button>
                    <button
                        onClick={() => setCurrentQuestionIndex(currentQuestionIndex + 1)}
                        disabled={currentQuestionIndex === questions.length - 1}
                        className="px-4 sm:px-6 py-2 sm:py-3 bg-blue-600 text-white rounded-xl shadow font-medium text-sm sm:text-base disabled:opacity-50 disabled:cursor-not-allowed hover:bg-blue-700"
                    >
                        Câu tiếp theo
                    </button>
                </div>

                {/* Lưới câu hỏi */}
                <div className="bg-white rounded-2xl shadow-lg p-4 sm:p-6">
                    <div className="flex items-center justify-between mb-4">
                        <h3 className="font-semibold text-sm sm:text-base">Danh sách câu hỏi</h3>
                        <div className="text-xs sm:text-sm text-gray-600">
                            Đã làm: {answeredCount}/{questions.length}
                        </div>
                    </div>
                    <div className="grid grid-cols-5 sm:grid-cols-10 gap-2">
                        {questions.map((question, index) => {
                            const isAnswered = selectedAnswers[index] !== null;
                            const isCurrent = index === currentQuestionIndex;
                            return (
                                <button
                                    key={question.id}
                                    onClick={() => setCurrentQuestionIndex(index)}
                                    className={`aspect-square rounded-lg font-semibold text-xs sm:text-sm flex items-center justify-center relative ${isCurrent
                                            ? 'bg-blue-600 text-white ring-2 ring-blue-300'
                                            : isAnswered
                                                ? 'bg-green-100 text-green-700 hover:bg-green-200'
                                                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                                        }`}
                                >
                                    {index + 1}
                                    {question.isImportant && (
                                        <Flag className="w-2.5 h-2.5 sm:w-3 sm:h-3 text-red-600 absolute top-0.5 right-0.5" />
                                    )}
                                </button>
                            );
                        })}
                    </div>
                </div>
            </div>

            {/* Modal xác nhận nộp bài */}
            {showConfirmSubmit && (
                <div className="fixed inset-0 bg-black/50 flex items-center justify-center p-4 z-50">
                    <div className="bg-white rounded-2xl max-w-md w-full p-4 sm:p-6">
                        <h3 className="text-lg sm:text-xl font-bold mb-3">Nộp bài thi?</h3>
                        <p className="text-sm sm:text-base text-gray-600 mb-2">
                            Bạn đã trả lời {answeredCount}/{questions.length} câu hỏi.
                        </p>
                        {answeredCount < questions.length && (
                            <p className="text-red-600 text-xs sm:text-sm mb-4">
                                Còn {questions.length - answeredCount} câu chưa trả lời sẽ được tính là sai.
                            </p>
                        )}
                        <div className="flex gap-3 mt-6">
                            <button
                                onClick={() => setShowConfirmSubmit(false)}
                                className="flex-1 px-4 py-2.5 sm:py-3 bg-gray-100 text-gray-700 rounded-xl text-sm sm:text-base font-medium hover:bg-gray-200"
                            >
                                Tiếp tục làm
                            </button>
                            <button
                                onClick={handleSubmit}
                                className="flex-1 px-4 py-2.5 sm:py-3 bg-blue-600 text-white rounded-xl text-sm sm:text-base font-medium hover:bg-blue-700"
                            >
                                Nộp bài
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

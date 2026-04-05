import { useState, useEffect } from 'react';
import { ChevronLeft, Search, Loader2, AlertCircle } from 'lucide-react';
import {
    fetchTrafficCategories,
    fetchTrafficSigns,
    type TrafficSign,
    type TrafficSignCategory
} from '../../services/api';

interface SignsLibraryProps {
    onBack: () => void;
}

export function SignsLibrary({ onBack }: SignsLibraryProps) {
    // 2. State quản lý dữ liệu API
    const [signs, setSigns] = useState<TrafficSign[]>([]);
    const [categories, setCategories] = useState<TrafficSignCategory[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // State quản lý tìm kiếm và lọc
    const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
    const [searchQuery, setSearchQuery] = useState('');

    useEffect(() => {
        const loadData = async () => {
            try {
                // Dùng Promise.all để gọi 2 API song song, giúp trang load nhanh gấp đôi
                const [signsData, categoriesData] = await Promise.all([
                    fetchTrafficSigns(),
                    fetchTrafficCategories()
                ]);

                setSigns(signsData);
                setCategories(categoriesData);
            } catch (err) {
                if (err instanceof Error) {
                    setError(err.message);
                } else {
                    setError('Đã có lỗi hệ thống xảy ra trong quá trình tải dữ liệu biển báo.');
                }
            } finally {
                setIsLoading(false);
            }
        };

        loadData();
    }, []);

    // Lọc dữ liệu dựa trên ô tìm kiếm và nút bấm loại biển báo
    const filteredSigns = signs.filter((sign) => {
        const matchesCategory = !selectedCategory || sign.categoryId === selectedCategory;
        const matchesSearch =
            !searchQuery ||
            sign.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
            sign.meaning.toLowerCase().includes(searchQuery.toLowerCase()) ||
            sign.id.toLowerCase().includes(searchQuery.toLowerCase());
        return matchesCategory && matchesSearch;
    });

    // Giữ nguyên hàm render Icon xịn xò của bồ (chỉ đổi sign.category thành sign.categoryId)
    const renderSignIcon = (sign: TrafficSign) => {
        // Nếu có link ảnh từ DB thì ưu tiên xài ảnh thật luôn cho đẹp
        if (sign.imageUrl) {
            return <img src={sign.imageUrl} alt={sign.name} className="w-20 h-20 sm:w-24 sm:h-24 object-contain" />;
        }

        // Backend đang lưu categoryId theo tiếng Việt (ví dụ: "Biển cấm").
        // Frontend trước đó lại so sánh với key tiếng Anh ("prohibition", "warning"...),
        // nên icon có thể không render. Map thêm các giá trị tương ứng để tương thích.
        if (['prohibition', 'Biển cấm', 'Cấm'].includes(sign.categoryId)) {
            return (
                <div className="relative w-full h-full flex items-center justify-center">
                    <div className="w-20 h-20 sm:w-24 sm:h-24 rounded-full border-4 border-red-600 bg-white flex items-center justify-center">
                        {sign.svgPath && (
                            <svg className="w-12 h-12 sm:w-14 sm:h-14 text-gray-800" viewBox="0 0 24 24" fill="currentColor">
                                <path d={sign.svgPath} />
                            </svg>
                        )}
                        <div className="absolute inset-0 flex items-center justify-center">
                            <div className="w-full h-1 bg-red-600 rotate-45" />
                        </div>
                    </div>
                </div>
            );
        }

        if (['warning', 'Biển báo nguy hiểm', 'Nguy hiểm'].includes(sign.categoryId)) {
            return (
                <div className="relative w-full h-full flex items-center justify-center">
                    <div className="w-0 h-0 border-l-[50px] border-r-[50px] border-b-[86px] sm:border-l-[60px] sm:border-r-[60px] sm:border-b-[104px] border-l-transparent border-r-transparent border-b-yellow-400 relative">
                        <div className="absolute -top-2 left-1/2 -translate-x-1/2 w-0 h-0 border-l-[46px] border-r-[46px] border-b-[79px] sm:border-l-[55px] sm:border-r-[55px] sm:border-b-[95px] border-l-transparent border-r-transparent border-b-white" />
                    </div>
                    <div className="absolute inset-0 flex items-center justify-center pt-4">
                        {sign.svgPath && (
                            <svg className="w-10 h-10 sm:w-12 sm:h-12 text-gray-800" viewBox="0 0 24 24" fill="currentColor">
                                <path d={sign.svgPath} />
                            </svg>
                        )}
                    </div>
                    <div className="absolute inset-0 flex items-center justify-center">
                        <div className="w-0 h-0 border-l-[50px] border-r-[50px] border-b-[86px] sm:border-l-[60px] sm:border-r-[60px] sm:border-b-[104px] border-l-transparent border-r-transparent border-b-transparent border-4 border-red-600" />
                    </div>
                </div>
            );
        }

        if (['mandatory', 'Biển hiệu lệnh', 'Hiệu lệnh'].includes(sign.categoryId)) {
            return (
                <div className="relative w-full h-full flex items-center justify-center">
                    <div className="w-20 h-20 sm:w-24 sm:h-24 rounded-full bg-blue-600 flex items-center justify-center">
                        {sign.svgPath ? (
                            <svg className="w-12 h-12 sm:w-14 sm:h-14 text-white" viewBox="0 0 24 24" fill="currentColor">
                                <path d={sign.svgPath} />
                            </svg>
                        ) : (
                            <div className="text-white font-bold text-xs">?</div>
                        )}
                    </div>
                </div>
            );
        }

        if (['information', 'Biển chỉ dẫn', 'Chỉ dẫn'].includes(sign.categoryId)) {
            return (
                <div className="relative w-full h-full flex items-center justify-center">
                    <div className="w-24 h-16 sm:w-28 sm:h-20 rounded-lg bg-blue-600 flex items-center justify-center border-2 border-white">
                        {sign.svgPath ? (
                            <svg className="w-10 h-10 sm:w-12 sm:h-12 text-white" viewBox="0 0 24 24" fill="currentColor">
                                <path d={sign.svgPath} />
                            </svg>
                        ) : (
                            <div className="text-white font-bold text-xs">{sign.id}</div>
                        )}
                    </div>
                </div>
            );
        }

        return null;
    };

    // MÀN HÌNH LOADING
    if (isLoading) {
        return (
            <div className="min-h-screen bg-gray-50 flex flex-col items-center justify-center text-blue-600">
                <Loader2 className="w-12 h-12 animate-spin mb-4" />
                <p className="font-semibold text-gray-600">Đang tải dữ liệu biển báo...</p>
            </div>
        );
    }

    // MÀN HÌNH LỖI (Ví dụ rớt mạng hoặc Backend chưa chạy)
    if (error) {
        return (
            <div className="min-h-screen bg-gray-50 flex flex-col items-center justify-center px-4 text-center">
                <div className="w-20 h-20 bg-red-100 text-red-600 rounded-full flex items-center justify-center mb-4">
                    <AlertCircle className="w-10 h-10" />
                </div>
                <h2 className="text-xl font-bold text-gray-900 mb-2">Không thể tải dữ liệu</h2>
                <p className="text-gray-600 mb-6">{error}</p>
                <button onClick={onBack} className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 font-medium">
                    Quay lại trang chủ
                </button>
            </div>
        );
    }

    // --- GIAO DIỆN CHÍNH (Đã load xong data) ---
    return (
        <div className="min-h-screen bg-gray-50">
            {/* Header */}
            <header className="bg-white shadow-sm sticky top-0 z-10">
                <div className="max-w-6xl mx-auto px-4 sm:px-6 py-4">
                    <button onClick={onBack} className="flex items-center gap-2 text-gray-600 hover:text-gray-900 mb-3">
                        <ChevronLeft className="w-5 h-5" />
                        <span className="font-medium">Quay lại</span>
                    </button>
                    <h1 className="text-xl sm:text-2xl font-bold text-gray-900">Biển báo giao thông</h1>
                    <p className="text-sm sm:text-base text-gray-600 mt-1">
                        {signs.length} biển báo
                    </p>
                </div>
            </header>

            <div className="max-w-6xl mx-auto px-4 sm:px-6 py-4 sm:py-6 space-y-4 sm:space-y-6">
                {/* Thanh tìm kiếm */}
                <div className="bg-white rounded-xl shadow p-4">
                    <div className="relative">
                        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400" />
                        <input
                            type="text"
                            placeholder="Tìm kiếm biển báo (VD: Cấm đi ngược chiều, P.102)..."
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            className="w-full pl-10 pr-4 py-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none"
                        />
                    </div>
                </div>

                {/* Nút lọc theo Category */}
                <div className="bg-white rounded-xl shadow p-4 sm:p-6">
                    <h2 className="font-semibold text-base sm:text-lg mb-4">Loại biển báo</h2>
                    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
                        <button
                            onClick={() => setSelectedCategory(null)}
                            className={`p-4 rounded-xl border-2 transition-all text-left ${selectedCategory === null ? 'border-blue-600 bg-blue-50' : 'border-gray-200 hover:border-blue-300'
                                }`}
                        >
                            <div className="font-semibold text-sm sm:text-base text-gray-900">Tất cả</div>
                            <div className="text-xs sm:text-sm text-gray-600 mt-1">{signs.length} biển báo</div>
                        </button>

                        {categories.map((category) => {
                            const count = signs.filter((s) => s.categoryId === category.id).length;
                            return (
                                <button
                                    key={category.id}
                                    onClick={() => setSelectedCategory(category.id)}
                                    className={`p-4 rounded-xl border-2 transition-all text-left ${selectedCategory === category.id ? 'border-blue-600 bg-blue-50' : 'border-gray-200 hover:border-blue-300'
                                        }`}
                                >
                                    <div className="font-semibold text-sm sm:text-base text-gray-900">{category.name}</div>
                                    <div className="text-xs sm:text-sm text-gray-600 mt-1">{count} biển báo</div>
                                    <div className="text-xs text-gray-500 mt-1 line-clamp-2">{category.description}</div>
                                </button>
                            );
                        })}
                    </div>
                </div>

                {/* Lưới danh sách biển báo */}
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 sm:gap-6">
                    {filteredSigns.map((sign) => {
                        const categoryInfo = categories.find((c) => c.id === sign.categoryId);
                        return (
                            <div key={sign.id} className="bg-white rounded-2xl shadow-lg overflow-hidden hover:shadow-xl transition-shadow">
                                <div className="h-40 sm:h-48 bg-gradient-to-br from-gray-100 to-gray-200 flex items-center justify-center p-6">
                                    {renderSignIcon(sign)}
                                </div>
                                <div className="p-4 sm:p-5">
                                    <div className="flex items-start justify-between mb-2">
                                        <div className="flex-1 pr-2">
                                            <h3 className="font-bold text-base sm:text-lg text-gray-900 mb-1">{sign.name}</h3>
                                            <p className="text-xs sm:text-sm text-gray-500 mb-2">{sign.id}</p>
                                        </div>
                                        {categoryInfo && (
                                            <span className={`px-2 py-1 rounded-full text-xs font-medium text-white bg-gradient-to-r ${categoryInfo.color}`}>
                                                {categoryInfo.name.split(' ').slice(-2).join(' ')}
                                            </span>
                                        )}
                                    </div>
                                    <p className="text-xs sm:text-sm text-gray-700 leading-relaxed line-clamp-3">
                                        {sign.meaning}
                                    </p>
                                </div>
                            </div>
                        );
                    })}
                </div>

                {/* Trạng thái Empty (Không tìm thấy) */}
                {filteredSigns.length === 0 && (
                    <div className="bg-white rounded-2xl shadow p-12 text-center">
                        <div className="text-gray-400 mb-3">
                            <Search className="w-12 h-12 mx-auto" />
                        </div>
                        <h3 className="text-lg font-semibold text-gray-900 mb-2">Không tìm thấy biển báo</h3>
                        <p className="text-gray-600">Thử tìm kiếm với từ khóa khác hoặc chọn loại biển báo khác</p>
                    </div>
                )}
            </div>
        </div>
    );
}
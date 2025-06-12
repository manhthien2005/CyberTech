let currentSubSubcategoryId = 0;
let currentPage = 1;

// Edit Product Variables
let editSelectedImages = [];
let editPrimaryImageIndex = -1;
let editExistingImages = [];
let editDeletedImageIds = [];

// Load categories hierarchically
function loadCategories() {
    console.log('Loading categories');
    $.ajax({
        url: '/ProductManage/GetCategories',
        method: 'GET',
        success: function (data) {
            console.log('Categories received:', data);
            renderCategories(data);
        },
        error: function (xhr, status, error) {
            console.error('Error loading categories:', error, xhr.status);
            $('#categoriesList').html('<li class="list-group-item">Không thể tải danh mục. Vui lòng thử lại.</li>');
        }
    });
}

function renderCategories(categories) {
    console.log('Rendering categories:', categories);
    const list = $('#categoriesList');
    list.empty();
    if (!categories || categories.length === 0) {
        list.append('<li class="list-group-item">Không có danh mục nào.</li>');
        return;
    }
    categories.forEach(category => {
        const categoryId = category.categoryID;
        const categoryItem = $(`
            <li class="category-item">
                <a href="#" data-bs-toggle="collapse" data-bs-target="#subcat-${categoryId}"><i class="fas fa-folder me-2"></i>${category.name}</a>
                <div class="category-actions">
                    <i class="fas fa-plus action-icon add" data-parentid="${categoryId}" data-level="subcategory" title="Thêm danh mục phụ"></i>
                    <i class="fas fa-edit action-icon edit" data-categoryid="${categoryId}" data-categoryname="${category.name}" title="Sửa danh mục"></i>
                    <i class="fas fa-trash action-icon delete" data-categoryid="${categoryId}" data-categoryname="${category.name}" title="Xóa danh mục"></i>
                </div>
                <ul class="collapse" id="subcat-${categoryId}">
                    ${renderSubcategories(category.subcategories, categoryId)}
                </ul>
            </li>
        `);
        list.append(categoryItem);
    });
}

function renderSubcategories(subcategories, parentId) {
    if (!subcategories || subcategories.length === 0) return '<li class="subcategory-item">Không có danh mục phụ.</li>';
    let html = '';
    subcategories.forEach(subcategory => {
        const subcategoryId = subcategory.subcategoryID;
        html += `
            <li class="subcategory-item">
                <a href="#" data-bs-toggle="collapse" data-bs-target="#subsubcat-${subcategoryId}"><i class="fas fa-folder-open me-2"></i>${subcategory.name}</a>
                <div class="category-actions">
                    <i class="fas fa-plus action-icon add" data-parentid="${subcategoryId}" data-level="subsubcategory" title="Thêm danh mục chi tiết"></i>
                    <i class="fas fa-edit action-icon edit" data-subcategoryid="${subcategoryId}" data-subcategoryname="${subcategory.name}" title="Sửa danh mục phụ"></i>
                    <i class="fas fa-trash action-icon delete" data-subcategoryid="${subcategoryId}" data-subcategoryname="${subcategory.name}" title="Xóa danh mục phụ"></i>
                </div>
                <ul class="collapse" id="subsubcat-${subcategoryId}">
                    ${renderSubSubcategories(subcategory.subSubcategories, subcategoryId)}
                </ul>
            </li>
        `;
    });
    return html;
}

function renderSubSubcategories(subsubcategories, parentId) {
    if (!subsubcategories || subsubcategories.length === 0) return '<li class="subsubcategory-item">Không có danh mục chi tiết.</li>';
    let html = '';
    subsubcategories.forEach(subsubcategory => {
        const subsubcategoryId = subsubcategory.subSubcategoryID;
        html += `
            <li class="subsubcategory-item">
                <a href="#" data-subsubcategoryid="${subsubcategoryId}"><i class="fas fa-file-alt me-2"></i>${subsubcategory.name}</a>
                <div class="category-actions">
                    <i class="fas fa-edit action-icon edit" data-subsubcategoryid="${subsubcategoryId}" data-subsubcategoryname="${subsubcategory.name}" title="Sửa danh mục chi tiết"></i>
                    <i class="fas fa-trash action-icon delete" data-subsubcategoryid="${subsubcategoryId}" data-subsubcategoryname="${subsubcategory.name}" title="Xóa danh mục chi tiết"></i>
                </div>
            </li>
        `;
    });
    return html;
}

// Load products with filters
function loadProducts(page = 1) {
    console.log('Loading products, page:', page);
    const search = $('#productSearchInput').val();
    const status = $('#statusFilter .active').data('status');
    const sort = $('#sortFilter').val();

    $.ajax({
        url: '/ProductManage/GetProducts',
        method: 'GET',
        data: {
            search: search,
            status: status,
            sort: sort,
            page: page,
            subsubcategoryId: currentSubSubcategoryId
        },
        success: function (data) {
            console.log('Products received:', data);
            renderProducts(data.products);
            renderPagination(data.totalPages, page);
        },
        error: function (xhr, status, error) {
            console.error('Error loading products:', error, xhr.status);
            $('#productsGrid').html('<div class="col">Không thể tải sản phẩm. Vui lòng thử lại.</div>');
        }
    });
}

function renderProducts(products) {
    console.log('Rendering products:', products);
    const grid = $('#productsGrid');
    grid.empty();
    if (!products || products.length === 0) {
        grid.append('<div class="col-12"><div class="empty-state"><i class="fas fa-box-open"></i><p>Không có sản phẩm nào.</p></div></div>');
        return;
    }
    products.forEach(product => {
        // Calculate discount percentage and pricing
        const isOnSale = product.isOnSale && product.originalPrice && product.price < product.originalPrice;
        const discountPercent = isOnSale ? Math.round(((product.originalPrice - product.price) / product.originalPrice) * 100) : 0;
        
        // Pricing HTML
        let pricingHTML = '';
        if (isOnSale) {
            pricingHTML = `
                <div class="product-pricing-section">
                    <div class="product-price-container on-sale">
                        <div>
                            <div class="product-price-original">${product.originalPrice.toLocaleString()} ₫</div>
                            <div class="product-price-current">${product.price.toLocaleString()} ₫</div>
                        </div>
                    </div>
                    <div class="product-discount-badge">-${discountPercent}%</div>
                </div>
            `;
        } else {
            pricingHTML = `
                <div class="product-pricing-section">
                    <div class="product-price-container">
                        <div class="product-price-regular">${product.price.toLocaleString()} ₫</div>
                    </div>
                </div>
            `;
        }
        
        // Attributes HTML
        /*let attributesHTML = '';
        if (product.attributes && product.attributes.length > 0) {
            const visibleAttributes = product.attributes.slice(0, 3);
            attributesHTML = `
                <div class="product-attributes-section">
                    <div class="product-attributes-title">
                        <i class="fas fa-cogs"></i> Thông số
                    </div>
                    <div class="product-attributes-grid">
                        ${visibleAttributes.map(attr => `
                            <div class="product-attribute-item">
                                <span class="product-attribute-name">${attr.name}</span>
                                <span class="product-attribute-value">${attr.value}</span>
                            </div>
                        `).join('')}
                        ${product.attributes.length > 3 ? `
                            <div class="product-attribute-item">
                                <span class="product-attribute-name">Xem thêm</span>
                                <span class="product-attribute-value">+${product.attributes.length - 3}</span>
                            </div>
                        ` : ''}
                    </div>
                </div>
            `;
        }*/
        
        const card = $(`
            <div class="col">
                <div class="card product-card">
                    <div class="product-image-container">
                        <img src="${product.imageUrl || '/images/placeholder.jpg'}" class="product-image" alt="${product.name}">
                        <div class="product-status-badge ${product.stock > 0 ? 'status-active' : 'status-inactive'}">
                            ${product.stock > 0 ? 'Còn hàng' : 'Hết hàng'}
                        </div>
                    </div>
                    
                    <div class="product-card-header">
                        <div class="product-card-title">${product.name}</div>
                        <div class="product-card-category">
                            <i class="fas fa-tag"></i> ${product.categoryName || 'Danh mục'}
                        </div>
                    </div>
                    
                    <div class="product-card-body">
                        <!-- Enhanced Pricing Section -->
                        ${pricingHTML}
                        
                        <!-- Brand Section -->
                        <div class="product-card-info">
                            <div class="product-card-info-label">
                                <i class="fas fa-star"></i> Thương hiệu
                            </div>
                            <div class="product-card-info-value">
                                <span class="product-brand">${product.brand || 'Không có'}</span>
                            </div>
                        </div>
                        
                        <!-- Stock Section -->
                        <div class="product-card-info">
                            <div class="product-card-info-label">
                                <i class="fas fa-warehouse"></i> Tồn kho
                            </div>
                            <div class="product-card-info-value">
                                <span class="product-stock-value ${product.stock > 0 ? 'in-stock' : 'out-of-stock'}">
                                    <i class="fas fa-${product.stock > 0 ? 'check-circle' : 'times-circle'}"></i>
                                    ${product.stock} sản phẩm
                                </span>
                            </div>
                        </div>
                    </div>
                    
                    <div class="product-card-footer">
                        <button class="btn btn-action btn-view view-product" data-id="${product.id}">
                            <i class="fas fa-eye me-1"></i> Xem
                        </button>
                        <button class="btn btn-action btn-edit edit-product" data-id="${product.id}">
                            <i class="fas fa-edit me-1"></i> Sửa
                        </button>
                        <button class="btn btn-action btn-delete delete-product" data-id="${product.id}" data-name="${product.name}">
                            <i class="fas fa-trash me-1"></i> Xóa
                        </button>
                    </div>
                </div>
            </div>
        `);
        grid.append(card);
    });
}

function renderPagination(totalPages, currentPage) {
    console.log('Rendering pagination, total pages:', totalPages);
    const paginationItems = $('#paginationItems');
    paginationItems.empty();
    if (totalPages === 0) {
        paginationItems.append('<li class="page-item disabled"><span class="page-link">Không có trang nào</span></li>');
        return;
    }
    for (let i = 1; i <= totalPages; i++) {
        const pageItem = $(`
            <li class="page-item ${i === currentPage ? 'active' : ''}">
                <a class="page-link" href="#">${i}</a>
            </li>
        `);
        pageItem.on('click', function (e) {
            e.preventDefault();
            currentPage = i;
            loadProducts(i);
        });
        paginationItems.append(pageItem);
    }
    $('#paginationPrev').toggleClass('disabled', currentPage === 1);
    $('#paginationNext').toggleClass('disabled', currentPage === totalPages);
}

// Event listeners for filters
$('#productSearchInput').on('input', function () {
    console.log('Search input changed:', $(this).val());
    currentPage = 1;
    loadProducts();
});

$('#statusFilter button').on('click', function () {
    $('#statusFilter button').removeClass('active');
    $(this).addClass('active');
    console.log('Status filter changed:', $(this).data('status'));
    currentPage = 1;
    loadProducts();
});

$('#sortFilter').on('change', function () {
    console.log('Sort filter changed:', $(this).val());
    currentPage = 1;
    loadProducts();
});

// Category selection
$('#categoriesList').on('click', 'a[data-subsubcategoryid]', function (e) {
    e.preventDefault();
    currentSubSubcategoryId = $(this).data('subsubcategoryid');
    console.log('Selected subsubcategory:', currentSubSubcategoryId);
    currentPage = 1;
    loadProducts();
});

// Add/Edit category/subcategory/subsubcategory
$('#categoriesList').on('click', '.action-icon.add', function () {
    const parentId = $(this).data('parentid');
    const level = $(this).data('level');
    $('#addCategoryModal').data('parentid', parentId);
    $('#addCategoryModal').data('level', level);
    $('#addCategoryModalLabel').text(`Thêm ${level === 'subcategory' ? 'Danh mục phụ' : level === 'subsubcategory' ? 'Danh mục chi tiết' : 'Danh mục'}`);
    $('#categoryName').val(''); if ($('#subsubcategoryList').length) { $('#subsubcategoryList').empty(); $('#subsubcategoryContainer').hide(); } if (level === 'subcategory') { if ($('#subsubcategoryContainer').length) { $('#subsubcategoryContainer').show(); } loadSubSubcategories(parentId); }
    $('#addCategoryModal').modal('show');
});

$('#addCategoryBtn').on('click', function () {
    $('#addCategoryModal').data('level', 'category');
    $('#addCategoryModalLabel').text('Thêm Danh mục');
    $('#categoryName').val('');
    $('#subsubcategoryList').empty();
    $('#addCategoryModal').modal('show');
});

function loadSubSubcategories(subcategoryId) { $.ajax({ url: '/ProductManage/GetSubSubcategories', method: 'GET', data: { subcategoryId: subcategoryId }, success: function (data) { console.log('SubSubcategories received:', data); renderSubSubcategoryList(data); }, error: function (xhr, status, error) { console.error('Error loading subsubcategories:', error, xhr.status); $('#subsubcategoryList').html('<div>Không thể tải danh mục chi tiết.</div>'); } }); }

function renderSubSubcategoryList(subsubcategories) {
    const list = $('#subsubcategoryList');
    list.empty();
    if (!subsubcategories || subsubcategories.length === 0) {
        list.append('<div class="text-muted">Không có danh mục chi tiết.</div>');
        return;
    }
    subsubcategories.forEach(subsubcategory => {
        const item = $(`
            <li class="list-group-item">
                <span>${subsubcategory.name}</span>
                <div class="category-actions">
                                        <i class="fas fa-edit action-icon edit" data-subsubcategoryid="${subsubcategory.id}" data-subsubcategoryname="${subsubcategory.name}" title="Sửa danh mục chi tiết"></i>                    <i class="fas fa-trash action-icon delete" data-subsubcategoryid="${subsubcategory.id}" data-subsubcategoryname="${subsubcategory.name}" title="Xóa danh mục chi tiết"></i>
                </div>
            </li>
        `);
        list.append(item);
    });
}

$('#saveCategoryBtn').on('click', function () {
    const name = $('#categoryName').val().trim();
    if (!name) {
        alert('Tên danh mục không được để trống.');
        return;
    }
    const level = $('#addCategoryModal').data('level');
    const parentId = $('#addCategoryModal').data('parentid');

    let url;
    let data = { name: name };
    if (level === 'category') {
        url = '/ProductManage/AddCategory';
    } else if (level === 'subcategory') {
        url = '/ProductManage/AddSubcategory';
        data.parentId = parentId;
    } else if (level === 'subsubcategory') {
        url = '/ProductManage/AddSubSubcategory';
        data.parentId = parentId;
    }

    $.ajax({
        url: url,
        method: 'POST',
        data: data,
        success: function () {
            console.log(`${level} added successfully`);
            $('#addCategoryModal').modal('hide');
            loadCategories();
            if (level === 'subcategory') {
                loadSubSubcategories(parentId);
            }
        },
        error: function (xhr, status, error) {
            console.error(`Error adding ${level}:`, error, xhr.status);
            alert(`Không thể thêm ${level === 'category' ? 'danh mục' : level === 'subcategory' ? 'danh mục phụ' : 'danh mục chi tiết'}.`);
        }
    });
});

// Edit category/subcategory/subsubcategory
$('#categoriesList').on('click', '.action-icon.edit', function () {
    const categoryId = $(this).data('categoryid');
    const subcategoryId = $(this).data('subcategoryid');
    const subsubcategoryId = $(this).data('subsubcategoryid');
    const name = $(this).data('categoryname') || $(this).data('subcategoryname') || $(this).data('subsubcategoryname');

    $('#editCategoryModal').data('categoryid', categoryId);
    $('#editCategoryModal').data('subcategoryid', subcategoryId);
    $('#editCategoryModal').data('subsubcategoryid', subsubcategoryId);
    $('#editCategoryName').val(name);
    $('#editCategoryModal').modal('show');
});

$('#saveEditCategoryBtn').on('click', function () {
    const categoryId = $('#editCategoryModal').data('categoryid');
    const subcategoryId = $('#editCategoryModal').data('subcategoryid');
    const subsubcategoryId = $('#editCategoryModal').data('subsubcategoryid');
    const name = $('#editCategoryName').val();

    let url = '';
    if (categoryId) {
        url = '/ProductManage/UpdateCategory';
        data = { id: categoryId, name: name };
    } else if (subcategoryId) {
        url = '/ProductManage/UpdateSubcategory';
        data = { id: subcategoryId, name: name };
    } else if (subsubcategoryId) {
        url = '/ProductManage/UpdateSubSubcategory';
        data = { id: subsubcategoryId, name: name };
    }

    $.ajax({
        url: url,
        method: 'PUT',
        data: data,
        success: function () {
            $('#editCategoryModal').modal('hide');
            loadCategories();
        },
        error: function (xhr, status, error) {
            console.error('Error updating category:', error);
            alert('Không thể cập nhật danh mục. Vui lòng thử lại.');
        }
    });
});

// Delete category/subcategory/subsubcategory
$('#categoriesList').on('click', '.action-icon.delete', function () {
    const categoryId = $(this).data('categoryid');
    const subcategoryId = $(this).data('subcategoryid');
    const subsubcategoryId = $(this).data('subsubcategoryid');
    const name = $(this).data('categoryname') || $(this).data('subcategoryname') || $(this).data('subsubcategoryname');

    if (categoryId) {
        $('#deleteCategoryModal #deleteCategoryName').text(name);
        $('#deleteCategoryModal #deleteCategoryId').val(categoryId);
        $('#deleteCategoryModal').modal('show');
    } else if (subcategoryId) {
        $('#deleteSubcategoryModal #deleteSubcategoryName').text(name);
        $('#deleteSubcategoryModal #deleteSubcategoryId').val(subcategoryId);
        $('#deleteSubcategoryModal').modal('show');
    } else if (subsubcategoryId) {
        $('#deleteSubSubcategoryModal #deleteSubSubcategoryName').text(name);
        $('#deleteSubSubcategoryModal #deleteSubSubcategoryId').val(subsubcategoryId);
        $('#deleteSubSubcategoryModal').modal('show');
    }
});

$('#subsubcategoryList').on('click', '.action-icon.delete', function () {
    const subsubcategoryId = $(this).data('subsubcategoryid');
    const subsubcategoryName = $(this).data('subsubcategoryname');
    $('#deleteSubSubcategoryModal #deleteSubSubcategoryName').text(subsubcategoryName);
    $('#deleteSubSubcategoryModal #deleteSubSubcategoryId').val(subsubcategoryId);
    $('#deleteSubSubcategoryModal').modal('show');
});

$('#confirmDeleteCategoryBtn').on('click', function () {
    const categoryId = $('#deleteCategoryId').val();
    $.ajax({
        url: '/ProductManage/DeleteCategory/' + categoryId,
        method: 'DELETE',
        success: function () {
            console.log('Category deleted');
            $('#deleteCategoryModal').modal('hide');
            loadCategories();
            loadProducts();
        },
        error: function (xhr, status, error) {
            console.error('Error deleting category:', error, xhr.status);
            alert('Không thể xóa danh mục.');
        }
    });
});

$('#confirmDeleteSubcategoryBtn').on('click', function () {
    const subcategoryId = $('#deleteSubcategoryId').val();
    $.ajax({
        url: '/ProductManage/DeleteSubcategory/' + subcategoryId,
        method: 'DELETE',
        success: function () {
            console.log('Subcategory deleted');
            $('#deleteSubcategoryModal').modal('hide');
            loadCategories();
            loadProducts();
        },
        error: function (xhr, status, error) {
            console.error('Error deleting subcategory:', error, xhr.status);
            alert('Không thể xóa danh mục phụ.');
        }
    });
});

$('#confirmDeleteSubSubcategoryBtn').on('click', function () {
    const subsubcategoryId = $('#deleteSubSubcategoryId').val();
    $.ajax({
        url: '/ProductManage/DeleteSubSubcategory/' + subsubcategoryId,
        method: 'DELETE',
        success: function () {
            console.log('SubSubcategory deleted');
            $('#deleteSubSubcategoryModal').modal('hide');
            loadCategories();
            loadProducts();
            if ($('#addCategoryModal').data('level') === 'subcategory') {
                loadSubSubcategories($('#addCategoryModal').data('id') || $('#addCategoryModal').data('parentid'));
            }
        },
        error: function (xhr, status, error) {
            console.error('Error deleting subsubcategory:', error, xhr.status);
            alert('Không thể xóa danh mục chi tiết.');
        }
    });
});

// Load categories for add product form
function loadCategoriesForAddProduct() {
    $.get('/ProductManage/GetCategories', function (categories) {
        const categorySelect = $('#productCategory');
        categorySelect.empty().append('<option value="" selected disabled>Chọn danh mục</option>');

        categories.forEach(category => {
            categorySelect.append(`<option value="${category.categoryID}">${category.name}</option>`);
        });

        // Reset and disable other dropdowns
        $('#productSubcategory')
            .empty()
            .append('<option value="" selected disabled>Chọn danh mục phụ</option>')
            .prop('disabled', true);

        $('#productSubSubcategory')
            .empty()
            .append('<option value="" selected disabled>Chọn danh mục chi tiết</option>')
            .prop('disabled', true);
    });
}

// Handle category change in add product form
$('#productCategory').change(function () {
    const categoryId = $(this).val();
    const subcategorySelect = $('#productSubcategory');
    const subSubcategorySelect = $('#productSubSubcategory');

    console.log('Category changed to:', categoryId);

    // Reset subcategory dropdown
    subcategorySelect
        .empty()
        .append('<option value="" selected disabled>Đang tải...</option>')
        .prop('disabled', true);

    // Reset subsubcategory dropdown
    subSubcategorySelect
        .empty()
        .append('<option value="" selected disabled>Chọn danh mục chi tiết</option>')
        .prop('disabled', true);

    if (categoryId) {
        $.ajax({
            url: '/ProductManage/GetSubcategories',
            method: 'GET',
            data: { categoryId: categoryId },
            success: function (subcategories) {
                console.log('Received subcategories:', subcategories);
                subcategorySelect.empty().append('<option value="" selected disabled>Chọn danh mục phụ</option>');

                if (subcategories && subcategories.length > 0) {
                    subcategories.forEach(subcategory => {
                        subcategorySelect.append(`<option value="${subcategory.id}">${subcategory.name}</option>`);
                    });
                    subcategorySelect.prop('disabled', false);
                } else {
                    subcategorySelect.append('<option value="" disabled>Không có danh mục phụ</option>');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading subcategories:', error);
                subcategorySelect.empty().append('<option value="" disabled>Lỗi khi tải danh mục phụ</option>');
                showToast('Không thể tải danh mục phụ. Vui lòng thử lại.', 'error');
            }
        });
    } else {
        subcategorySelect.empty().append('<option value="" selected disabled>Chọn danh mục phụ</option>');
    }
});

// Handle subcategory change in add product form
$('#productSubcategory').change(function () {
    const subcategoryId = $(this).val();
    const subSubcategorySelect = $('#productSubSubcategory');

    console.log('Subcategory changed to:', subcategoryId);

    // Reset subsubcategory dropdown
    subSubcategorySelect
        .empty()
        .append('<option value="" selected disabled>Đang tải...</option>')
        .prop('disabled', true);

    if (subcategoryId) {
        $.ajax({
            url: '/ProductManage/GetSubSubcategories',
            method: 'GET',
            data: { subcategoryId: subcategoryId },
            success: function (subSubcategories) {
                console.log('Received subsubcategories:', subSubcategories);
                subSubcategorySelect.empty().append('<option value="" selected disabled>Chọn danh mục chi tiết</option>');

                if (subSubcategories && subSubcategories.length > 0) {
                    subSubcategories.forEach(subSubcategory => {
                        console.log('Processing subsubcategory:', subSubcategory);
                        subSubcategorySelect.append(`<option value="${subSubcategory.id}">${subSubcategory.name}</option>`);
                    });
                    subSubcategorySelect.prop('disabled', false);
                } else {
                    subSubcategorySelect.append('<option value="" disabled>Không có danh mục chi tiết</option>');
                    showToast('Danh mục phụ này chưa có danh mục chi tiết nào', 'warning');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading subsubcategories:', error);
                console.error('Response:', xhr.responseText);
                subSubcategorySelect.empty().append('<option value="" disabled>Lỗi khi tải danh mục chi tiết</option>');
                showToast('Không thể tải danh mục chi tiết. Vui lòng thử lại.', 'error');
            }
        });
    } else {
        subSubcategorySelect.empty().append('<option value="" selected disabled>Chọn danh mục chi tiết</option>');
    }
});

// Load categories when add product modal is shown
$('#addProductModal').on('show.bs.modal', function () {
    console.log('Add product modal opening');
    resetProductForm();
    loadCategoriesForAddProduct();
});

// Handle modal close event
$('#addProductModal').on('hidden.bs.modal', function () {
    console.log('Add product modal closed');
    resetProductForm();
});

// Handle product status change
$('#productStatus').change(function () {
    const stockInput = $('#productStock');
    if ($(this).val() === 'active') {
        stockInput.prop('disabled', false).val('');
    } else {
        stockInput.prop('disabled', true).val('0');
    }
});

// Handle image upload
let selectedImages = [];
let primaryImageIndex = -1;

// Add Product Image Upload
$('#uploadImageBtn').click(function () {
    $('#productImage').click();
});

$('#productImage').change(function (e) {
    const files = Array.from(e.target.files);
    if (files.length > 0) {
        // Validate file types and sizes
        const validFiles = [];
        for (let file of files) {
            if (!file.type.startsWith('image/')) {
                showToast(`File "${file.name}" không phải là ảnh`, 'error');
                continue;
            }
            if (file.size > 5 * 1024 * 1024) {
                showToast(`Ảnh "${file.name}" vượt quá 5MB`, 'error');
                continue;
            }
            validFiles.push(file);
        }
        
        if (validFiles.length > 0) {
            selectedImages = selectedImages.concat(validFiles);
            updateImagePreview();
        }
    }
});

// Drag and drop for Add Product
$('#uploadArea').on('dragover dragenter', function(e) {
    e.preventDefault();
    e.stopPropagation();
    $(this).addClass('drag-over');
});

$('#uploadArea').on('dragleave dragend', function(e) {
    e.preventDefault();
    e.stopPropagation();
    $(this).removeClass('drag-over');
});

$('#uploadArea').on('drop', function(e) {
    e.preventDefault();
    e.stopPropagation();
    $(this).removeClass('drag-over');
    
    const files = Array.from(e.originalEvent.dataTransfer.files);
    if (files.length > 0) {
        const validFiles = [];
        for (let file of files) {
            if (!file.type.startsWith('image/')) {
                showToast(`File "${file.name}" không phải là ảnh`, 'error');
                continue;
            }
            if (file.size > 5 * 1024 * 1024) {
                showToast(`Ảnh "${file.name}" vượt quá 5MB`, 'error');
                continue;
            }
            validFiles.push(file);
        }
        
        if (validFiles.length > 0) {
            selectedImages = selectedImages.concat(validFiles);
            updateImagePreview();
        }
    }
});

function updateImagePreview() {
    const container = $('#imagePreviewContainer');
    const grid = $('#imagePreviewGrid');
    
    if (selectedImages.length === 0) {
        container.hide();
        return;
    }
    
    container.show();
    grid.empty();
    
    selectedImages.forEach((file, index) => {
        const reader = new FileReader();
        reader.onload = function(e) {
            const imageItem = $(`
                <div class="col-6 col-md-4">
                    <div class="image-preview-item position-relative">
                        <img src="${e.target.result}" class="img-fluid rounded" style="height: 120px; object-fit: cover; width: 100%;" alt="Preview">
                        <div class="image-overlay">
                            <button type="button" class="btn btn-sm btn-danger remove-image" data-index="${index}">
                                <i class="fas fa-trash"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-info view-image" data-index="${index}">
                                <i class="fas fa-eye"></i>
                            </button>
                        </div>
                        <div class="form-check position-absolute top-0 start-0 m-2">
                            <input class="form-check-input primary-image-radio" type="radio" 
                                name="primaryImage" value="${index}" 
                                ${index === primaryImageIndex ? 'checked' : ''}
                                ${index === 0 && primaryImageIndex === -1 ? 'checked' : ''}>
                            <label class="form-check-label text-white">
                                <small>Ảnh chính</small>
                            </label>
                        </div>
                        <div class="image-name small text-center p-1 bg-dark bg-opacity-75 text-white">
                            ${file.name}
                        </div>
                    </div>
                </div>
            `);
            grid.append(imageItem);
        };
        reader.readAsDataURL(file);
    });
    
    // Set first image as primary if none selected
    if (primaryImageIndex === -1 && selectedImages.length > 0) {
        primaryImageIndex = 0;
    }
}

// Handle primary image selection
$(document).on('change', '.primary-image-radio', function () {
    primaryImageIndex = parseInt($(this).val());
});

// Handle image viewing
$(document).on('click', '.view-image', function () {
    const index = $(this).data('index');
    const file = selectedImages[index];
    const reader = new FileReader();

    reader.onload = function (e) {
        const modal = $(`
            <div class="modal fade" id="imagePreviewModal" tabindex="-1">
                <div class="modal-dialog modal-lg modal-dialog-centered">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">Xem trước ảnh: ${file.name}</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body text-center">
                            <img src="${e.target.result}" class="img-fluid" alt="Preview">
                        </div>
                    </div>
                </div>
            </div>
        `);
        $('body').append(modal);
        modal.modal('show');
        modal.on('hidden.bs.modal', function() {
            $(this).remove();
        });
    };
    reader.readAsDataURL(file);
});

// Handle image removal
$(document).on('click', '.remove-image', function () {
    const index = $(this).data('index');
    selectedImages.splice(index, 1);
    
    // Adjust primary image index
    if (primaryImageIndex === index) {
        primaryImageIndex = selectedImages.length > 0 ? 0 : -1;
    } else if (primaryImageIndex > index) {
        primaryImageIndex--;
    }
    
    updateImagePreview();
});

// Handle status change for Add Product
$('#productStatus').change(function() {
    const status = $(this).val();
    const stockInput = $('#productStock');
    
    if (status === 'Active') {
        stockInput.prop('disabled', false);
        if (stockInput.val() == '0') {
            stockInput.val('1');
        }
    } else {
        stockInput.prop('disabled', true).val('0');
    }
});

// Handle sale toggle for Add Product
$('#enableSale').change(function() {
    const isEnabled = $(this).is(':checked');
    $('#saleOptions').toggle(isEnabled);
    
    if (!isEnabled) {
        $('#productSalePrice').val('');
        $('#productSalePercent').val('');
        $('#effectivePricePreview').addClass('d-none');
    }
});

// Handle sale type change for Add Product
$('input[name="saleType"]').change(function() {
    const saleType = $(this).val();
    
    if (saleType === 'price') {
        $('#salePriceContainer').show();
        $('#salePercentContainer').hide();
        $('#productSalePercent').val('');
    } else {
        $('#salePriceContainer').hide();
        $('#salePercentContainer').show();
        $('#productSalePrice').val('');
    }
    updatePricePreview();
});

// Update price preview for Add Product
function updatePricePreview() {
    const originalPrice = parseFloat($('#productPrice').val()) || 0;
    const saleType = $('input[name="saleType"]:checked').val();
    let finalPrice = 0;
    
    if (saleType === 'price') {
        finalPrice = parseFloat($('#productSalePrice').val()) || 0;
    } else if (saleType === 'percent') {
        const percent = parseFloat($('#productSalePercent').val()) || 0;
        finalPrice = originalPrice * (1 - percent / 100);
    }
    
    if (finalPrice > 0 && finalPrice < originalPrice) {
        $('#previewPrice').text(finalPrice.toLocaleString() + ' ₫');
        $('#effectivePricePreview').removeClass('d-none');
    } else {
        $('#effectivePricePreview').addClass('d-none');
    }
}

// Bind price change events for Add Product
$('#productPrice, #productSalePrice, #productSalePercent').on('input', updatePricePreview);

// Edit Product Variables và handlers
// Handle edit product click
$(document).on('click', '.edit-product', function () {
    const productId = $(this).data('id');
    console.log('Edit product clicked, ID:', productId);
    loadProductForEdit(productId);
});

// Load product data for editing
function loadProductForEdit(productId) {
    console.log('Loading product for edit:', productId);

    $.ajax({
        url: '/ProductManage/GetProduct',
        method: 'GET',
        data: { id: productId },
        success: function (product) {
            console.log('Product data loaded:', product);
            populateEditForm(product);
            $('#editProductModal').modal('show');
        },
        error: function (xhr, status, error) {
            console.error('Error loading product:', error);
            showToast('Không thể tải thông tin sản phẩm', 'error');
        }
    });
}

// Populate edit form with product data
function populateEditForm(product) {
    console.log('Populating edit form with:', product);

    // Basic info
    $('#editProductId').val(product.id);
    $('#editProductName').val(product.name);
    $('#editProductDescription').val(product.description);
    $('#editProductPrice').val(product.price);
    $('#editProductStock').val(product.stock);
    $('#editProductBrand').val(product.brand);
    $('#editProductStatus').val(product.isActive ? 'Active' : 'Inactive');

    // Handle stock field based on status
    if (product.isActive) {
        $('#editProductStock').prop('disabled', false);
    } else {
        $('#editProductStock').prop('disabled', true).val('0');
    }

    // Handle sale information
    if (product.salePrice && product.salePrice < product.price) {
        $('#editEnableSale').prop('checked', true);
        $('#editSaleOptions').show();
        $('#editProductSalePrice').val(product.salePrice);
        $('#editEffectivePricePreview').removeClass('d-none');
        $('#editPreviewPrice').text(product.salePrice.toLocaleString() + ' ₫');
    } else {
        $('#editEnableSale').prop('checked', false);
        $('#editSaleOptions').hide();
    }

    // Reset and load categories
    loadCategoriesForEdit(product.categoryId, product.subcategoryId, product.subSubcategoryId);

    // Reset images
    editSelectedImages = [];
    editPrimaryImageIndex = -1;
    editExistingImages = product.images || [];
    editDeletedImageIds = [];
    updateEditCurrentImages();

    // Load attributes
    loadAttributesForEdit(product.attributes || []);
}

// Load categories for edit form with preselection
function loadCategoriesForEdit(selectedCategoryId, selectedSubcategoryId, selectedSubSubcategoryId) {
    $.get('/ProductManage/GetCategories', function (categories) {
        const categorySelect = $('#editProductCategory');
        categorySelect.empty().append('<option value="" selected disabled>Chọn danh mục</option>');

        categories.forEach(category => {
            const isSelected = category.categoryID === selectedCategoryId ? 'selected' : '';
            categorySelect.append(`<option value="${category.categoryID}" ${isSelected}>${category.name}</option>`);
        });

        // Load subcategories if category is selected
        if (selectedCategoryId) {
            loadSubcategoriesForEdit(selectedCategoryId, selectedSubcategoryId, selectedSubSubcategoryId);
        }
    });
}

// Load subcategories for edit
function loadSubcategoriesForEdit(categoryId, selectedSubcategoryId, selectedSubSubcategoryId) {
    $.ajax({
        url: '/ProductManage/GetSubcategories',
        method: 'GET',
        data: { categoryId: categoryId },
        success: function (subcategories) {
            const subcategorySelect = $('#editProductSubcategory');
            subcategorySelect.empty().append('<option value="" disabled>Chọn danh mục phụ</option>');

            if (subcategories && subcategories.length > 0) {
                subcategories.forEach(subcategory => {
                    const isSelected = subcategory.id === selectedSubcategoryId ? 'selected' : '';
                    subcategorySelect.append(`<option value="${subcategory.id}" ${isSelected}>${subcategory.name}</option>`);
                });
                subcategorySelect.prop('disabled', false);

                // Load subsubcategories if subcategory is selected
                if (selectedSubcategoryId) {
                    loadSubSubcategoriesForEdit(selectedSubcategoryId, selectedSubSubcategoryId);
                }
            }
        },
        error: function () {
            showToast('Không thể tải danh mục phụ', 'error');
        }
    });
}

// Load subsubcategories for edit
function loadSubSubcategoriesForEdit(subcategoryId, selectedSubSubcategoryId) {
    $.ajax({
        url: '/ProductManage/GetSubSubcategories',
        method: 'GET',
        data: { subcategoryId: subcategoryId },
        success: function (subSubcategories) {
            const subSubcategorySelect = $('#editProductSubSubcategory');
            subSubcategorySelect.empty().append('<option value="" disabled>Chọn danh mục chi tiết</option>');

            if (subSubcategories && subSubcategories.length > 0) {
                subSubcategories.forEach(subSubcategory => {
                    const isSelected = subSubcategory.id === selectedSubSubcategoryId ? 'selected' : '';
                    subSubcategorySelect.append(`<option value="${subSubcategory.id}" ${isSelected}>${subSubcategory.name}</option>`);
                });
                subSubcategorySelect.prop('disabled', false);
            }
        },
        error: function () {
            showToast('Không thể tải danh mục chi tiết', 'error');
        }
    });
}

// Load attributes for edit
function loadAttributesForEdit(attributes) {
    const container = $('#editProductAttributes');
    container.empty();

    attributes.forEach(attr => {
        const attributeHtml = `
            <div class="attribute-item mb-2">
                <div class="input-group">
                    <input type="text" class="form-control" placeholder="Tên thuộc tính" value="${attr.name}" name="attributes[].name">
                    <input type="text" class="form-control" placeholder="Giá trị" value="${attr.value}" name="attributes[].value">
                    <button type="button" class="btn btn-outline-danger remove-attribute" title="Xóa thuộc tính">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </div>
        `;
        container.append(attributeHtml);
    });
}

// Update edit current images
function updateEditCurrentImages() {
    const container = $('#editCurrentImages');
    container.empty();

    if (editExistingImages.length === 0) {
        container.append('<div class="col-12"><p class="text-muted text-center">Không có ảnh hiện tại</p></div>');
        return;
    }

    editExistingImages.forEach((image, index) => {
        if (!editDeletedImageIds.includes(image.id)) {
            const isPrimarySelected = image.isPrimary || (editPrimaryImageIndex === `existing-${index}`);
            
            const imageItem = $(`
                <div class="col-6 col-md-4">
                    <div class="image-preview-item position-relative current-image ${isPrimarySelected ? 'primary-selected' : ''}" data-image-id="${image.id}">
                        <img src="${image.url}" class="img-fluid rounded" style="height: 120px; object-fit: cover; width: 100%;" alt="Current Image">
                        <div class="image-overlay">
                            <button type="button" class="btn btn-sm btn-danger delete-current-image" data-image-id="${image.id}">
                                <i class="fas fa-trash"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-info view-current-image" data-image-url="${image.url}">
                                <i class="fas fa-eye"></i>
                            </button>
                        </div>
                        <div class="form-check position-absolute top-0 start-0 m-2">
                            <input class="form-check-input edit-primary-image-radio" type="radio" 
                                name="editPrimaryImage" value="existing-${index}" 
                                ${isPrimarySelected ? 'checked' : ''}>
                            <label class="form-check-label text-white">
                                <small>Ảnh chính</small>
                            </label>
                        </div>
                        <div class="image-name small text-center p-1 bg-dark bg-opacity-75 text-white">
                            ${image.fileName || 'Ảnh hiện tại'}
                        </div>
                    </div>
                </div>
            `);
            container.append(imageItem);
            
            // Set editPrimaryImageIndex if this is the primary image
            if (isPrimarySelected && !editPrimaryImageIndex) {
                editPrimaryImageIndex = `existing-${index}`;
                console.log('Set primary image index to:', editPrimaryImageIndex);
            }
        }
    });
}

// Handle edit category change
$('#editProductCategory').change(function () {
    const categoryId = $(this).val();
    loadSubcategoriesForEdit(categoryId, null, null);

    // Reset lower level dropdowns
    $('#editProductSubSubcategory').empty().append('<option value="" disabled>Chọn danh mục chi tiết</option>').prop('disabled', true);
});

// Handle edit subcategory change
$('#editProductSubcategory').change(function () {
    const subcategoryId = $(this).val();
    loadSubSubcategoriesForEdit(subcategoryId, null);
});

// Handle edit attribute buttons
$('#editAddAttributeBtn').click(function () {
    const attributeHtml = `
        <div class="attribute-item mb-2">
            <div class="input-group">
                <input type="text" class="form-control" placeholder="Tên thuộc tính (VD: Màu sắc)" name="attributes[].name">
                <input type="text" class="form-control" placeholder="Giá trị (VD: Đỏ)" name="attributes[].value">
                <button type="button" class="btn btn-outline-danger remove-attribute" title="Xóa thuộc tính">
                    <i class="fas fa-trash"></i>
                </button>
            </div>
        </div>
    `;
    $('#editProductAttributes').append(attributeHtml);
});

// Handle existing image actions
$(document).on('click', '.delete-current-image', function () {
    const imageId = $(this).data('image-id');
    const imageIndex = editExistingImages.findIndex(img => img.id === imageId);
    
    // Check if deleting the primary image
    const wasPrimary = editPrimaryImageIndex === `existing-${imageIndex}` || 
                      editExistingImages[imageIndex]?.isPrimary;
    
    editDeletedImageIds.push(imageId);
    console.log('Deleted image ID:', imageId, 'Was primary:', wasPrimary);
    
    // If deleted image was primary, reset primary selection
    if (wasPrimary) {
        editPrimaryImageIndex = -1;
        console.log('Reset primary image index due to deletion');
    }
    
    updateEditCurrentImages();
    
    // Auto-select first remaining image as primary if needed
    if (wasPrimary) {
        setTimeout(() => {
            const firstRadio = $('input[name="editPrimaryImage"]:first');
            if (firstRadio.length > 0) {
                firstRadio.prop('checked', true).trigger('change');
                console.log('Auto-selected first remaining image as primary');
            }
        }, 100);
    }
});

$(document).on('click', '.view-current-image', function () {
    const imageUrl = $(this).data('image-url');
    showImagePreview(imageUrl, 'Ảnh hiện tại');
});

// Handle edit modal events
$('#editProductModal').on('show.bs.modal', function () {
    console.log('Edit product modal opening');
});

$('#editProductModal').on('hidden.bs.modal', function () {
    console.log('Edit product modal closed');
    resetEditForm();
});

// Reset edit form
function resetEditForm() {
    $('#editProductForm')[0].reset();
    $('#editProductStock').prop('disabled', true);
    $('#editProductSubcategory').prop('disabled', true);
    $('#editProductSubSubcategory').prop('disabled', true);
    $('#editProductAttributes').empty();
    $('#editEnableSale').prop('checked', false);
    $('#editSaleOptions').hide();
    $('#editEffectivePricePreview').addClass('d-none');
    editSelectedImages = [];
    editPrimaryImageIndex = -1;
    editExistingImages = [];
    editDeletedImageIds = [];
    updateEditCurrentImages();
    updateEditImagePreview();
}

// Handle update product button click
$('#updateProductBtn').click(function () {
    if (!validateEditProductForm()) {
        return;
    }

    const formData = new FormData();

    // Basic product info
    formData.append('ProductID', $('#editProductId').val());
    formData.append('Name', $('#editProductName').val());
    formData.append('Description', $('#editProductDescription').val() || '');
    formData.append('Price', $('#editProductPrice').val());
    formData.append('Stock', $('#editProductStock').val() || '0');
    formData.append('SubSubcategoryID', $('#editProductSubSubcategory').val());
    formData.append('Brand', $('#editProductBrand').val() || '');

    // Status
    const status = $('#editProductStatus').val();
    const isActive = status === 'Active';
    formData.append('IsActive', isActive);

    // Sale information
    if ($('#editEnableSale').is(':checked')) {
        const saleType = $('input[name="editSaleType"]:checked').val();
        if (saleType === 'price') {
            const salePrice = parseFloat($('#editProductSalePrice').val());
            if (salePrice > 0) {
                formData.append('SalePrice', salePrice);
            }
        } else if (saleType === 'percent') {
            const originalPrice = parseFloat($('#editProductPrice').val());
            const percent = parseFloat($('#editProductSalePercent').val());
            if (percent > 0 && percent <= 100) {
                const salePrice = originalPrice * (1 - percent / 100);
                formData.append('SalePrice', salePrice);
            }
        }
    }

    // Deleted image IDs
    if (editDeletedImageIds.length > 0) {
        formData.append('DeletedImageIds', editDeletedImageIds.join(','));
    }

    // New images
    editSelectedImages.forEach((file, index) => {
        formData.append('images', file);
    });

    // Primary image index - FIXED LOGIC
    console.log('Current editPrimaryImageIndex:', editPrimaryImageIndex);
    
    // Get currently selected primary image radio
    const selectedPrimaryRadio = $('input[name="editPrimaryImage"]:checked');
    if (selectedPrimaryRadio.length > 0) {
        const primaryValue = selectedPrimaryRadio.val();
        console.log('Selected primary radio value:', primaryValue);
        
        if (primaryValue.startsWith('existing-')) {
            // Primary is an existing image
            const existingIndex = parseInt(primaryValue.split('-')[1]);
            const actualImage = editExistingImages[existingIndex];
            if (actualImage && !editDeletedImageIds.includes(actualImage.id)) {
                // Map to the actual remaining position
                const remainingExistingImages = editExistingImages.filter(img => !editDeletedImageIds.includes(img.id));
                const actualIndex = remainingExistingImages.findIndex(img => img.id === actualImage.id);
                formData.append('PrimaryImageIndex', actualIndex);
                console.log('Setting primary to existing image at index:', actualIndex);
            }
        } else if (primaryValue.startsWith('new-')) {
            // Primary is a new image
            const newIndex = parseInt(primaryValue.split('-')[1]);
            const remainingExistingCount = editExistingImages.filter(img => !editDeletedImageIds.includes(img.id)).length;
            const finalIndex = remainingExistingCount + newIndex;
            formData.append('PrimaryImageIndex', finalIndex);
            console.log('Setting primary to new image at final index:', finalIndex);
        }
    } else {
        // No primary selected, ensure first remaining image is primary
        const remainingExistingImages = editExistingImages.filter(img => !editDeletedImageIds.includes(img.id));
        if (remainingExistingImages.length > 0 || editSelectedImages.length > 0) {
            formData.append('PrimaryImageIndex', 0);
            console.log('No primary selected, defaulting to index 0');
        }
    }

    // Attributes
    const attributes = [];
    $('#editProductAttributes .attribute-item').each(function () {
        const name = $(this).find('input[name*="name"]').val().trim();
        const value = $(this).find('input[name*="value"]').val().trim();
        if (name && value) {
            attributes.push({ name: name, value: value });
        }
    });
    if (attributes.length > 0) {
        formData.append('Attributes', JSON.stringify(attributes));
    }

    // Show loading
    $('#updateProductBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Đang cập nhật...');

    // Submit form
    $.ajax({
        url: '/ProductManage/UpdateProduct',
        type: 'PUT',
        data: formData,
        processData: false,
        contentType: false,
        success: function (response) {
            console.log('Product updated successfully:', response);
            $('#editProductModal').modal('hide');
            loadProducts();
            showToast('Cập nhật sản phẩm thành công!', 'success');
        },
        error: function (xhr, status, error) {
            console.error('Error updating product:', error);
            console.error('Response:', xhr.responseText);

            let errorMessage = 'Lỗi khi cập nhật sản phẩm';

            if (xhr.status === 400) {
                try {
                    const response = JSON.parse(xhr.responseText);
                    if (response.errors) {
                        const errors = Object.values(response.errors).flat();
                        errorMessage = errors.join('. ');
                    } else if (response.message) {
                        errorMessage = response.message;
                    } else if (typeof response === 'string') {
                        errorMessage = response;
                    }
                } catch (e) {
                    errorMessage = xhr.responseText || 'Dữ liệu không hợp lệ';
                }
            } else if (xhr.status === 500) {
                errorMessage = 'Lỗi server. Vui lòng thử lại sau.';
            } else if (xhr.responseText) {
                errorMessage = xhr.responseText;
            }

            showToast(errorMessage, 'error');
        },
        complete: function () {
            $('#updateProductBtn').prop('disabled', false).html('<i class="fas fa-save me-2"></i>Cập nhật sản phẩm');
        }
    });
});

// Validate edit product form
function validateEditProductForm() {
    // Check product name
    const productName = $('#editProductName').val().trim();
    if (!productName) {
        showToast('Vui lòng nhập tên sản phẩm', 'error');
        $('#editProductName').focus();
        return false;
    }
    if (productName.length < 3) {
        showToast('Tên sản phẩm phải có ít nhất 3 ký tự', 'error');
        $('#editProductName').focus();
        return false;
    }

    // Check categories
    if (!$('#editProductCategory').val()) {
        showToast('Vui lòng chọn danh mục chính', 'error');
        $('#editProductCategory').focus();
        return false;
    }
    if (!$('#editProductSubcategory').val()) {
        showToast('Vui lòng chọn danh mục phụ', 'error');
        $('#editProductSubcategory').focus();
        return false;
    }
    if (!$('#editProductSubSubcategory').val()) {
        showToast('Vui lòng chọn danh mục chi tiết', 'error');
        $('#editProductSubSubcategory').focus();
        return false;
    }

    // Check price
    const price = parseFloat($('#editProductPrice').val());
    if (!price || price <= 0) {
        showToast('Vui lòng nhập giá sản phẩm hợp lệ (lớn hơn 0)', 'error');
        $('#editProductPrice').focus();
        return false;
    }
    if (price > 999999999) {
        showToast('Giá sản phẩm không được vượt quá 999,999,999 đ', 'error');
        $('#editProductPrice').focus();
        return false;
    }

    // Check sale information
    if ($('#editEnableSale').is(':checked')) {
        const saleType = $('input[name="editSaleType"]:checked').val();
        if (saleType === 'price') {
            const salePrice = parseFloat($('#editProductSalePrice').val());
            if (!salePrice || salePrice <= 0) {
                showToast('Vui lòng nhập giá khuyến mãi hợp lệ', 'error');
                $('#editProductSalePrice').focus();
                return false;
            }
            if (salePrice >= price) {
                showToast('Giá khuyến mãi phải nhỏ hơn giá gốc', 'error');
                $('#editProductSalePrice').focus();
                return false;
            }
        } else if (saleType === 'percent') {
            const percent = parseFloat($('#editProductSalePercent').val());
            if (!percent || percent <= 0 || percent > 100) {
                showToast('Vui lòng nhập phần trăm giảm từ 1-100', 'error');
                $('#editProductSalePercent').focus();
                return false;
            }
        }
    }

    // Check status and stock
    const status = $('#editProductStatus').val();
    const stock = parseInt($('#editProductStock').val()) || 0;

    if (!status) {
        showToast('Vui lòng chọn trạng thái sản phẩm', 'error');
        $('#editProductStatus').focus();
        return false;
    }

    if (status === 'Active' && stock <= 0) {
        showToast('Sản phẩm hoạt động phải có số lượng lớn hơn 0', 'error');
        $('#editProductStock').focus();
        return false;
    }

    if (stock < 0) {
        showToast('Số lượng không được âm', 'error');
        $('#editProductStock').focus();
        return false;
    }

    // Check if at least one image remains (existing + new - deleted)
    const remainingExistingImages = editExistingImages.filter(img => !editDeletedImageIds.includes(img.id));
    const totalImages = remainingExistingImages.length + editSelectedImages.length;

    if (totalImages === 0) {
        showToast('Sản phẩm phải có ít nhất một ảnh', 'error');
        return false;
    }

    if (editSelectedImages.length > 10) {
        showToast('Chỉ được tải lên tối đa 10 ảnh mới', 'error');
        return false;
    }

    // Check file sizes for new images
    for (let i = 0; i < editSelectedImages.length; i++) {
        const file = editSelectedImages[i];
        if (file.size > 5 * 1024 * 1024) { // 5MB
            showToast(`Ảnh "${file.name}" vượt quá 5MB. Vui lòng chọn ảnh nhỏ hơn.`, 'error');
            return false;
        }
    }

    // Check attributes
    const attributeItems = $('#editProductAttributes .attribute-item');
    let hasInvalidAttribute = false;

    attributeItems.each(function () {
        const name = $(this).find('input[name*="name"]').val().trim();
        const value = $(this).find('input[name*="value"]').val().trim();

        if ((name && !value) || (!name && value)) {
            showToast('Thuộc tính phải có đầy đủ tên và giá trị', 'error');
            hasInvalidAttribute = true;
            return false;
        }
    });

    if (hasInvalidAttribute) {
        return false;
    }

    return true;
}

// Handle view product click
$(document).on('click', '.view-product', function () {
    const productId = $(this).data('id');
    console.log('View product clicked, ID:', productId);
    loadProductForView(productId);
});

// Load product data for viewing
function loadProductForView(productId) {
    console.log('Loading product for view:', productId);

    $.ajax({
        url: '/ProductManage/GetProduct',
        method: 'GET',
        data: { id: productId },
        success: function (product) {
            console.log('Product data loaded for view:', product);
            populateViewModal(product);
            $('#viewProductModal').modal('show');
        },
        error: function (xhr, status, error) {
            console.error('Error loading product for view:', error);
            showToast('Không thể tải thông tin sản phẩm', 'error');
        }
    });
}

// Populate view modal with product data
function populateViewModal(product) {
    console.log('Populating view modal with:', product);

    // Basic info
    $('#viewProductName').text(product.name);
    $('#viewCategoryPath').text(product.categoryName || 'Danh mục');
    $('#viewBrand').text(product.brand || 'Không có');
    $('#viewStock').text(`${product.stock} sản phẩm`);
    $('#viewStatus').html(`<span class="badge ${product.isActive ? 'bg-success' : 'bg-danger'}">${product.isActive ? 'Còn hàng' : 'Hết hàng'}</span>`);
    $('#viewDescription').html(product.description || '<em class="text-muted">Không có mô tả</em>');

    // Store product ID for edit button
    $('#editFromViewBtn').data('product-id', product.id);

    // Pricing section
    const isOnSale = product.isActive && product.salePrice && product.salePrice < product.price;
    const discountPercent = isOnSale ? Math.round(((product.price - product.salePrice) / product.price) * 100) : 0;
    
    let pricingHTML = '';
    if (isOnSale) {
        pricingHTML = `
            <div class="d-flex align-items-center justify-content-between">
                <div>
                    <div class="text-decoration-line-through text-muted mb-1" style="font-size: 1.1rem;">${product.price.toLocaleString()} ₫</div>
                    <div class="fs-2 fw-bold text-danger">${product.salePrice.toLocaleString()} ₫</div>
                </div>
                <div>
                    <span class="badge bg-warning text-dark fs-6">Giảm ${discountPercent}%</span>
                </div>
            </div>
        `;
    } else {
        pricingHTML = `<div class="fs-2 fw-bold text-dark">${product.price.toLocaleString()} ₫</div>`;
    }
    $('#viewPricingSection').html(pricingHTML);

    // Images
    if (product.images && product.images.length > 0) {
        const primaryImage = product.images.find(img => img.isPrimary) || product.images[0];
        $('#viewMainImage').attr('src', primaryImage.url).attr('alt', product.name);

        // Thumbnails
        const thumbnailsHTML = product.images.map((image, index) => `
            <div class="thumbnail-item ${image.isPrimary ? 'active' : ''}" data-image-url="${image.url}">
                <img src="${image.url}" alt="Ảnh ${index + 1}">
            </div>
        `).join('');
        $('#viewImageThumbnails').html(thumbnailsHTML);
    } else {
        $('#viewMainImage').attr('src', '/images/placeholder.jpg').attr('alt', 'Không có ảnh');
        $('#viewImageThumbnails').empty();
    }

    // Attributes
    if (product.attributes && product.attributes.length > 0) {
        const attributesHTML = product.attributes.map(attr => `
            <div class="attribute-grid-item">
                <span class="attribute-grid-name">${attr.name}</span>
                <span class="attribute-grid-value">${attr.value}</span>
            </div>
        `).join('');
        $('#viewAttributes').html(attributesHTML);
        $('#viewAttributesSection').show();
    } else {
        $('#viewAttributes').html('<p class="text-muted">Không có thông số kỹ thuật</p>');
        $('#viewAttributesSection').show();
    }
}

// Handle thumbnail click in view modal
$(document).on('click', '.thumbnail-item', function () {
    const imageUrl = $(this).data('image-url');
    $('#viewMainImage').attr('src', imageUrl);
    $('.thumbnail-item').removeClass('active');
    $(this).addClass('active');
});

// Handle edit from view modal
$('#editFromViewBtn').click(function () {
    const productId = $(this).data('product-id');
    $('#viewProductModal').modal('hide');
    loadProductForEdit(productId);
});

// Initial load
$(document).ready(function () {
    console.log('Document ready');
    loadCategories();
    loadProducts();
});

function showToast(message, type = 'info') {
    // Tạo toast container nếu chưa có
    if ($('.toast-container').length === 0) {
        $('body').append('<div class="toast-container position-fixed top-0 end-0 p-3" style="z-index: 9999;"></div>');
    }

    const toastId = 'toast-' + Date.now();
    const iconClass = type === 'success' ? 'fas fa-check-circle text-success' :
        type === 'error' ? 'fas fa-times-circle text-danger' :
            type === 'warning' ? 'fas fa-exclamation-triangle text-warning' :
                'fas fa-info-circle text-info';

    const toast = $(`
        <div id="${toastId}" class="toast" role="alert" aria-live="assertive" aria-atomic="true" data-bs-delay="4000">
            <div class="toast-header">
                <i class="${iconClass} me-2"></i>
                <strong class="me-auto">Thông báo</strong>
                <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        </div>
    `);

    $('.toast-container').append(toast);

    // Sử dụng Bootstrap toast
    const bsToast = new bootstrap.Toast(toast[0]);
    bsToast.show();

    // Tự động xóa sau khi ẩn
    toast.on('hidden.bs.toast', function () {
        $(this).remove();
    });
}

// Handle form submission
$('#saveProductBtn').click(function () {
    if (!validateProductForm()) {
        return;
    }

    const formData = new FormData();

    // Thông tin cơ bản
    formData.append('Name', $('#productName').val());
    formData.append('Description', $('#productDescription').val() || '');
    formData.append('Price', $('#productPrice').val());
    formData.append('Stock', $('#productStock').val() || '0');
    formData.append('SubSubcategoryID', $('#productSubSubcategory').val());
    formData.append('Brand', $('#productBrand').val() || '');

    // Trạng thái sản phẩm
    const status = $('#productStatus').val();
    const isActive = status === 'Active';
    formData.append('IsActive', isActive);

    // Thông tin khuyến mãi
    if ($('#enableSale').is(':checked')) {
        const saleType = $('input[name="saleType"]:checked').val();
        if (saleType === 'price') {
            const salePrice = parseFloat($('#productSalePrice').val());
            if (salePrice > 0) {
                formData.append('SalePrice', salePrice);
            }
        } else if (saleType === 'percent') {
            const originalPrice = parseFloat($('#productPrice').val());
            const percent = parseFloat($('#productSalePercent').val());
            if (percent > 0 && percent <= 100) {
                const salePrice = originalPrice * (1 - percent / 100);
                formData.append('SalePrice', salePrice);
            }
        }
    }

    console.log('Submitting form with SubSubcategoryID:', $('#productSubSubcategory').val());
    console.log('Product status:', status, 'IsActive:', isActive);

    // Thuộc tính sản phẩm
    const attributes = [];
    $('#productAttributes .attribute-item').each(function () {
        const name = $(this).find('input[name*="name"]').val();
        const value = $(this).find('input[name*="value"]').val();
        if (name && value) {
            attributes.push({ name: name, value: value });
        }
    });
    if (attributes.length > 0) {
        formData.append('Attributes', JSON.stringify(attributes));
    }

    // Hình ảnh
    selectedImages.forEach((file, index) => {
        formData.append('images', file);
    });

    // Đánh dấu ảnh chính
    if (primaryImageIndex >= 0) {
        formData.append('PrimaryImageIndex', primaryImageIndex);
    }

    // Show loading
    $('#saveProductBtn').prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Đang xử lý...');

    // Submit form
    $.ajax({
        url: '/ProductManage/AddProduct',
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function (response) {
            console.log('Product added successfully:', response);
            $('#addProductModal').modal('hide');
            loadProducts();
            showToast('Thêm sản phẩm thành công!', 'success');
            resetProductForm();
        },
        error: function (xhr, status, error) {
            console.error('Error adding product:', error);
            console.error('Response:', xhr.responseText);
            console.error('Status:', xhr.status);

            let errorMessage = 'Lỗi khi thêm sản phẩm';

            if (xhr.status === 400) {
                try {
                    const response = JSON.parse(xhr.responseText);
                    if (response.errors) {
                        const errors = Object.values(response.errors).flat();
                        errorMessage = errors.join('. ');
                    } else if (response.message) {
                        errorMessage = response.message;
                    } else if (typeof response === 'string') {
                        errorMessage = response;
                    }
                } catch (e) {
                    errorMessage = xhr.responseText || 'Dữ liệu không hợp lệ';
                }
            } else if (xhr.status === 500) {
                errorMessage = 'Lỗi server. Vui lòng thử lại sau.';
            } else if (xhr.responseText) {
                errorMessage = xhr.responseText;
            }

            showToast(errorMessage, 'error');
        },
        complete: function () {
            $('#saveProductBtn').prop('disabled', false).html('<i class="fas fa-save me-2"></i>Lưu sản phẩm');
        }
    });
});

// Cập nhật hàm validate
function validateProductForm() {
    // Kiểm tra tên sản phẩm
    const productName = $('#productName').val().trim();
    if (!productName) {
        showToast('Vui lòng nhập tên sản phẩm', 'error');
        $('#productName').focus();
        return false;
    }
    if (productName.length < 3) {
        showToast('Tên sản phẩm phải có ít nhất 3 ký tự', 'error');
        $('#productName').focus();
        return false;
    }

    // Kiểm tra danh mục
    if (!$('#productCategory').val()) {
        showToast('Vui lòng chọn danh mục chính', 'error');
        $('#productCategory').focus();
        return false;
    }
    if (!$('#productSubcategory').val()) {
        showToast('Vui lòng chọn danh mục phụ', 'error');
        $('#productSubcategory').focus();
        return false;
    }
    if (!$('#productSubSubcategory').val()) {
        showToast('Vui lòng chọn danh mục chi tiết', 'error');
        $('#productSubSubcategory').focus();
        return false;
    }

    // Kiểm tra giá
    const price = parseFloat($('#productPrice').val());
    if (!price || price <= 0) {
        showToast('Vui lòng nhập giá sản phẩm hợp lệ (lớn hơn 0)', 'error');
        $('#productPrice').focus();
        return false;
    }
    if (price > 999999999) {
        showToast('Giá sản phẩm không được vượt quá 999,999,999 đ', 'error');
        $('#productPrice').focus();
        return false;
    }

    // Kiểm tra khuyến mãi
    if ($('#enableSale').is(':checked')) {
        const saleType = $('input[name="saleType"]:checked').val();
        if (saleType === 'price') {
            const salePrice = parseFloat($('#productSalePrice').val());
            if (!salePrice || salePrice <= 0) {
                showToast('Vui lòng nhập giá khuyến mãi hợp lệ', 'error');
                $('#productSalePrice').focus();
                return false;
            }
            if (salePrice >= price) {
                showToast('Giá khuyến mãi phải nhỏ hơn giá gốc', 'error');
                $('#productSalePrice').focus();
                return false;
            }
        } else if (saleType === 'percent') {
            const percent = parseFloat($('#productSalePercent').val());
            if (!percent || percent <= 0 || percent > 100) {
                showToast('Vui lòng nhập phần trăm giảm từ 1-100', 'error');
                $('#productSalePercent').focus();
                return false;
            }
        }
    }

    // Kiểm tra trạng thái và số lượng
    const status = $('#productStatus').val();
    const stock = parseInt($('#productStock').val()) || 0;

    if (!status) {
        showToast('Vui lòng chọn trạng thái sản phẩm', 'error');
        $('#productStatus').focus();
        return false;
    }

    if (status === 'Active' && stock <= 0) {
        showToast('Sản phẩm hoạt động phải có số lượng lớn hơn 0', 'error');
        $('#productStock').focus();
        return false;
    }

    if (stock < 0) {
        showToast('Số lượng không được âm', 'error');
        $('#productStock').focus();
        return false;
    }

    // Kiểm tra hình ảnh
    if (selectedImages.length === 0) {
        showToast('Vui lòng chọn ít nhất một ảnh sản phẩm', 'error');
        $('#uploadImageBtn').focus();
        return false;
    }

    if (selectedImages.length > 10) {
        showToast('Chỉ được tải lên tối đa 10 ảnh', 'error');
        return false;
    }

    if (primaryImageIndex === -1 && selectedImages.length > 0) {
        // Tự động chọn ảnh đầu tiên làm ảnh chính
        primaryImageIndex = 0;
        $('input[name="primaryImage"][value="0"]').prop('checked', true);
        showToast('Đã tự động chọn ảnh đầu tiên làm ảnh chính', 'info');
    }

    // Kiểm tra kích thước file ảnh
    for (let i = 0; i < selectedImages.length; i++) {
        const file = selectedImages[i];
        if (file.size > 5 * 1024 * 1024) { // 5MB
            showToast(`Ảnh "${file.name}" vượt quá 5MB. Vui lòng chọn ảnh nhỏ hơn.`, 'error');
            return false;
        }
    }

    // Kiểm tra thuộc tính (nếu có)
    const attributeItems = $('#productAttributes .attribute-item');
    let hasInvalidAttribute = false;

    attributeItems.each(function () {
        const name = $(this).find('input[name*="name"]').val().trim();
        const value = $(this).find('input[name*="value"]').val().trim();

        if ((name && !value) || (!name && value)) {
            showToast('Thuộc tính phải có đầy đủ tên và giá trị', 'error');
            hasInvalidAttribute = true;
            return false;
        }
    });

    if (hasInvalidAttribute) {
        return false;
    }

    return true;
}

function resetProductForm() {
    $('#addProductForm')[0].reset();

    // Reset trạng thái các field
    $('#productStock').prop('disabled', true).val('0');
    $('#productSubcategory').prop('disabled', true).empty().append('<option value="" selected disabled>Chọn danh mục phụ</option>');
    $('#productSubSubcategory').prop('disabled', true).empty().append('<option value="" selected disabled>Chọn danh mục chi tiết</option>');

    // Reset khuyến mãi
    $('#enableSale').prop('checked', false);
    $('#saleOptions').hide();
    $('#effectivePricePreview').addClass('d-none');
    $('input[name="saleType"][value="price"]').prop('checked', true);
    $('#salePriceContainer').show();
    $('#salePercentContainer').hide();

    // Reset thuộc tính
    $('#productAttributes').empty();

    // Reset hình ảnh
    selectedImages = [];
    primaryImageIndex = -1;
    updateImagePreview();

    // Reset file input
    $('#productImage').val('');

    // Reload categories
    loadCategoriesForAddProduct();

    console.log('Product form reset successfully');
}

// Handle add attribute button
$('#addAttributeBtn').click(function () {
    const attributeHtml = `
        <div class="attribute-item mb-2">
            <div class="input-group">
                <input type="text" class="form-control" placeholder="Tên thuộc tính (VD: Màu sắc)" name="attributes[].name">
                <input type="text" class="form-control" placeholder="Giá trị (VD: Đỏ)" name="attributes[].value">
                <button type="button" class="btn btn-outline-danger remove-attribute" title="Xóa thuộc tính">
                    <i class="fas fa-trash"></i>
                </button>
            </div>
        </div>
    `;
    $('#productAttributes').append(attributeHtml);
});

// Handle remove attribute
$(document).on('click', '.remove-attribute', function () {
    $(this).closest('.attribute-item').remove();
});

// Handle edit image upload button
$('#editUploadImageBtn').click(function () {
    $('#editProductImage').click();
});

// Handle edit image file selection
$('#editProductImage').change(function (e) {
    const files = Array.from(e.target.files);
    if (files.length > 0) {
        // Validate file types and sizes
        const validFiles = [];
        for (let file of files) {
            if (!file.type.startsWith('image/')) {
                showToast(`File "${file.name}" không phải là ảnh`, 'error');
                continue;
            }
            if (file.size > 5 * 1024 * 1024) {
                showToast(`Ảnh "${file.name}" vượt quá 5MB`, 'error');
                continue;
            }
            validFiles.push(file);
        }
        
        if (validFiles.length > 0) {
            editSelectedImages = editSelectedImages.concat(validFiles);
            updateEditImagePreview();
        }
    }
});

// Drag and drop for Edit Product
$('#editUploadArea').on('dragover dragenter', function(e) {
    e.preventDefault();
    e.stopPropagation();
    $(this).addClass('drag-over');
});

$('#editUploadArea').on('dragleave dragend', function(e) {
    e.preventDefault();
    e.stopPropagation();
    $(this).removeClass('drag-over');
});

$('#editUploadArea').on('drop', function(e) {
    e.preventDefault();
    e.stopPropagation();
    $(this).removeClass('drag-over');
    
    const files = Array.from(e.originalEvent.dataTransfer.files);
    if (files.length > 0) {
        const validFiles = [];
        for (let file of files) {
            if (!file.type.startsWith('image/')) {
                showToast(`File "${file.name}" không phải là ảnh`, 'error');
                continue;
            }
            if (file.size > 5 * 1024 * 1024) {
                showToast(`Ảnh "${file.name}" vượt quá 5MB`, 'error');
                continue;
            }
            validFiles.push(file);
        }
        
        if (validFiles.length > 0) {
            editSelectedImages = editSelectedImages.concat(validFiles);
            updateEditImagePreview();
        }
    }
});

// Update edit image preview
function updateEditImagePreview() {
    const container = $('#editImagePreviewContainer');
    const grid = $('#editImagePreviewGrid');
    
    if (editSelectedImages.length === 0) {
        container.hide();
        return;
    }
    
    container.show();
    grid.empty();
    
    editSelectedImages.forEach((file, index) => {
        const reader = new FileReader();
        reader.onload = function(e) {
            const imageItem = $(`
                <div class="col-6 col-md-4">
                    <div class="image-preview-item position-relative">
                        <img src="${e.target.result}" class="img-fluid rounded" style="height: 120px; object-fit: cover; width: 100%;" alt="Preview">
                        <div class="image-overlay">
                            <button type="button" class="btn btn-sm btn-danger remove-edit-image" data-index="${index}">
                                <i class="fas fa-trash"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-info view-edit-image" data-index="${index}">
                                <i class="fas fa-eye"></i>
                            </button>
                        </div>
                        <div class="form-check position-absolute top-0 start-0 m-2">
                            <input class="form-check-input edit-primary-image-radio" type="radio" 
                                name="editPrimaryImage" value="new-${index}">
                            <label class="form-check-label text-white">
                                <small>Ảnh chính</small>
                            </label>
                        </div>
                        <div class="image-name small text-center p-1 bg-dark bg-opacity-75 text-white">
                            ${file.name}
                        </div>
                    </div>
                </div>
            `);
            grid.append(imageItem);
        };
        reader.readAsDataURL(file);
    });
}

// Handle edit primary image selection
$(document).on('change', '.edit-primary-image-radio', function () {
    editPrimaryImageIndex = $(this).val();
    console.log('Primary image changed to:', editPrimaryImageIndex);
    
    // Visual feedback - highlight selected primary image
    $('.edit-primary-image-radio').closest('.image-preview-item').removeClass('primary-selected');
    $(this).closest('.image-preview-item').addClass('primary-selected');
});

// Handle edit image viewing
$(document).on('click', '.view-edit-image', function () {
    const index = $(this).data('index');
    const file = editSelectedImages[index];
    showImagePreview(URL.createObjectURL(file), file.name);
});

// Handle edit image removal
$(document).on('click', '.remove-edit-image', function () {
    const index = $(this).data('index');
    editSelectedImages.splice(index, 1);
    
    // Adjust primary image index for new images
    if (editPrimaryImageIndex === `new-${index}`) {
        editPrimaryImageIndex = -1;
    } else if (editPrimaryImageIndex && editPrimaryImageIndex.startsWith('new-')) {
        const currentIndex = parseInt(editPrimaryImageIndex.split('-')[1]);
        if (currentIndex > index) {
            editPrimaryImageIndex = `new-${currentIndex - 1}`;
        }
    }
    
    updateEditImagePreview();
});

// Handle edit status change
$('#editProductStatus').change(function() {
    const status = $(this).val();
    const stockInput = $('#editProductStock');
    
    if (status === 'Active') {
        stockInput.prop('disabled', false);
        if (stockInput.val() == '0') {
            stockInput.val('1');
        }
    } else {
        stockInput.prop('disabled', true).val('0');
    }
});

// Handle edit sale toggle
$('#editEnableSale').change(function() {
    const isEnabled = $(this).is(':checked');
    $('#editSaleOptions').toggle(isEnabled);
    
    if (!isEnabled) {
        $('#editProductSalePrice').val('');
        $('#editProductSalePercent').val('');
        $('#editEffectivePricePreview').addClass('d-none');
    }
});

// Handle edit sale type change
$('input[name="editSaleType"]').change(function() {
    const saleType = $(this).val();
    
    if (saleType === 'price') {
        $('#editSalePriceContainer').show();
        $('#editSalePercentContainer').hide();
        $('#editProductSalePercent').val('');
    } else {
        $('#editSalePriceContainer').hide();
        $('#editSalePercentContainer').show();
        $('#editProductSalePrice').val('');
    }
    updateEditPricePreview();
});

// Update edit price preview
function updateEditPricePreview() {
    const originalPrice = parseFloat($('#editProductPrice').val()) || 0;
    const saleType = $('input[name="editSaleType"]:checked').val();
    let finalPrice = 0;
    
    if (saleType === 'price') {
        finalPrice = parseFloat($('#editProductSalePrice').val()) || 0;
    } else if (saleType === 'percent') {
        const percent = parseFloat($('#editProductSalePercent').val()) || 0;
        finalPrice = originalPrice * (1 - percent / 100);
    }
    
    if (finalPrice > 0 && finalPrice < originalPrice) {
        $('#editPreviewPrice').text(finalPrice.toLocaleString() + ' ₫');
        $('#editEffectivePricePreview').removeClass('d-none');
    } else {
        $('#editEffectivePricePreview').addClass('d-none');
    }
}

// Bind edit price change events
$('#editProductPrice, #editProductSalePrice, #editProductSalePercent').on('input', updateEditPricePreview);

// Helper function to show image preview
function showImagePreview(imageUrl, imageName) {
    const modal = $(`
        <div class="modal fade" id="imagePreviewModal" tabindex="-1">
            <div class="modal-dialog modal-lg modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Xem trước ảnh: ${imageName}</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body text-center">
                        <img src="${imageUrl}" class="img-fluid" alt="Preview">
                    </div>
                </div>
            </div>
        </div>
    `);
    $('body').append(modal);
    modal.modal('show');
    modal.on('hidden.bs.modal', function() {
        $(this).remove();
    });
}

// ... existing code ...
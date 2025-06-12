1. Luồng đăng ký và đăng nhập

- Đăng ký tài khoản → Xác thực email → Đăng nhập
  @startuml

  actor User
  participant "Register View" as RegisterView
  participant "Login View" as LoginView
  participant "Controller" as Controller
  participant "User Service" as UserService
  participant "Database" as DB

== Account Registration ==
User -> RegisterView: Enters name, username, email, password
RegisterView -> Controller: Submits registration form
Controller -> UserService: Validates email and username
UserService -> DB: Checks if email, username exist
DB --> UserService: Does not exist
UserService --> Controller: OK
Controller -> UserService: Creates new account
UserService -> DB: Saves account
DB --> UserService: Save successful
UserService --> Controller: Registration successful
Controller -> RegisterView: Redirects to login page
RegisterView --> User: Displays "Registration successful"

== Login ==
User -> LoginView: Enters email, password
LoginView -> Controller: Submits login form
Controller -> UserService: Verifies login credentials
UserService -> DB: Authenticates email, password
DB --> UserService: Credentials are valid
UserService --> Controller: Returns account
Controller -> LoginView: Redirects to home page
LoginView --> User: Displays home page
@enduml

2. Luồng mua hàng

- Duyệt sản phẩm → Thêm vào giỏ hàng → Áp dụng voucher → Thanh toán → Xác nhận đơn hàng (cập nhật rank?)
  @startuml
  actor User
  participant "Home View" as HomeView
  participant "Product View" as ProductView
  participant "Cart View" as CartView
  participant "CartController" as CartController
  participant "PaymentController" as PaymentController
  participant "Cart Service" as CartService
  participant "Database" as DB

== Browse products and add to cart ==
User -> HomeView: Views products
HomeView -> User: Redirects to product detail page
User -> ProductView: Clicks "Add to cart"
ProductView -> CartController: POST /Cart/AddToCart
CartController -> CartService: Checks product and cart
CartService -> DB: Checks stock, gets cart
DB --> CartService: Product is available
CartService -> DB: Adds product to cart
DB --> CartService: Save successful
CartService --> CartController: OK
CartController --> ProductView: Notifies "Added to cart"
ProductView --> User: Displays notification

== View cart and apply voucher ==
User -> CartView: Views cart
CartView -> CartController: GET /Cart/Index
CartController -> CartService: Gets cart and addresses
CartService -> DB: Gets cart info, vouchers, addresses
DB --> CartService: Cart data
CartService --> CartController: OK
CartController --> CartView: Displays cart
User -> CartView: Enters voucher code (optional)
CartView -> CartController: POST /Cart/ApplyVoucher
CartController -> CartService: Checks and applies voucher
CartService -> DB: Checks if voucher is valid
DB --> CartService: Voucher is valid
CartService -> DB: Saves voucher to session
DB --> CartService: Save successful
CartService --> CartController: OK
CartController --> CartView: Updates total price

== Select address and checkout ==
User -> CartView: Selects address, chooses payment method (COD/VNPay)
CartView -> CartController: POST /Cart/Checkout
CartController -> CartService: Creates order
CartService -> DB: Saves order, updates voucher (if any)
DB --> CartService: Order created successfully
CartService --> CartController: OK, returns orderId
alt COD Payment
CartController -> PaymentController: POST /Payment/ProcessCODPayment
PaymentController -> DB: Updates payment and order status
DB --> PaymentController: Save successful
PaymentController --> CartController: OK
CartController --> CartView: Notifies "Order placed successfully"
CartView --> User: Redirects to order page
else VNPay Payment
CartController --> CartView: Returns orderId
CartView -> PaymentController: POST /Payment/ProcessVNPayPayment
PaymentController -> DB: Checks order
DB --> PaymentController: Order is valid
PaymentController --> CartView: Returns payment URL
CartView --> User: Redirects to VNPay
User -> PaymentController: Pays on VNPay
PaymentController -> DB: Updates payment and order status
DB --> PaymentController: Save successful
PaymentController --> CartView: Redirects to success page
end

== Update rank ==
PaymentController -> DB: Updates TotalSpent, OrderCount
DB --> PaymentController: Gets new rank (if achieved)
PaymentController -> DB: Updates user rank
DB --> PaymentController: Save successful
PaymentController -> User: Sends notification email (if rank changes)
CartView --> User: Displays order history page
@enduml

3. Luồng tương tác với chatbot

- Gửi câu hỏi → Xử lý câu hỏi → Trả lời thông tin sản phẩm
  @startuml
  skinparam DefaultFontSize 25
  actor User
  participant "Chat Widget" as ChatBox
  participant "ChatController" as Controller
  participant "Database" as DB
  participant "Gemini API" as Gemini

== Send Question ==
User -> ChatBox: Enters question
ChatBox -> Controller: POST /api/Chat/GeminiChat
Controller -> DB: Gets product and voucher info
DB --> Controller: Product and voucher data
Controller -> Gemini: Sends question + data
Gemini --> Controller: Returns response (Markdown)
Controller -> ChatBox: Converts to HTML
ChatBox --> User: Displays answer
@enduml

4. compare

@startuml
skinparam DefaultFontSize 25
actor User
participant "Product/Home View" as View
participant "Compare View" as CompareView
participant "ProductCompareController" as Controller
participant "Session" as Session
database "Database" as DB

== Add product to compare list ==
User -> View: Clicks "Compare" on a product
View -> Controller: POST /ProductCompare/AddToCompare
Controller -> Session: GetCompareProductIds()
Session --> Controller: Returns current list of productIds
Controller -> DB: Fetches product info for validation
DB --> Controller: Returns product info
alt Limit reached or incompatible
Controller --> View: JSON { success: false, message }
View --> User: Displays error message
else Compatible and valid
Controller -> Session: SetCompareProductIds(new list)
Session --> Controller: Save successful
Controller --> View: JSON { success: true, message, compareCount }
View --> User: Displays "Added" and updates counter
end

== View comparison page ==
User -> CompareView: Accesses the compare page
CompareView -> Controller: GET /ProductCompare/Index
Controller -> Session: GetCompareProductIds()
Session --> Controller: Returns list of productIds
Controller -> DB: Fetches details for all products in the list
DB --> Controller: Product data
Controller -> Controller: GenerateProductAnalysis(), GetTechnicalSpecs()
Controller --> CompareView: Returns ViewModel with analyzed data
CompareView --> User: Displays detailed comparison table and recommendation
@enduml

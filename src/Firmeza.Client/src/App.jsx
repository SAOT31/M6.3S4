import { useState, useEffect } from 'react'
import { ShoppingCart, LogOut, User, Search, ShoppingBag, CheckCircle, Download, FileText, Trash2, ArrowRight } from 'lucide-react'
import './App.css'

const API_BASE_URL = 'http://localhost:5000/api'

function App() {
  // Estado de Autenticación
  const [token, setToken] = useState(localStorage.getItem('token') || '')
  const [user, setUser] = useState(() => {
    const saved = localStorage.getItem('user')
    return saved ? JSON.parse(saved) : null
  })
  
  // Perfil de Cliente
  const [clientId, setClientId] = useState(() => {
    return localStorage.getItem('clientId') || ''
  })
  const [clientProfile, setClientProfile] = useState(null)
  const [needsProfileSetup, setNeedsProfileSetup] = useState(false)
  
  // Vistas y Navegación
  const [activeTab, setActiveTab] = useState('catalog') // 'catalog' | 'history'
  const [authMode, setAuthMode] = useState('login') // 'login' | 'register'
  
  // Formularios de Autenticación
  const [loginEmail, setLoginEmail] = useState('')
  const [loginPassword, setLoginPassword] = useState('')
  const [regEmail, setRegEmail] = useState('')
  const [regPassword, setRegPassword] = useState('')
  const [regName, setRegName] = useState('')
  
  // Formulario de Perfil
  const [profileForm, setProfileForm] = useState({
    firstName: '',
    lastName: '',
    documentType: 'CC',
    documentNumber: '',
    phone: '',
    address: '',
    age: ''
  })
  
  // Catálogo de Productos
  const [products, setProducts] = useState([])
  const [searchQuery, setSearchQuery] = useState('')
  const [selectedCategory, setSelectedCategory] = useState('All')
  const [categories, setCategories] = useState(['All'])
  
  // Carrito de Compras
  const [cart, setCart] = useState([])
  const [isCartOpen, setIsCartOpen] = useState(false)
  
  // Historial de Compras
  const [salesHistory, setSalesHistory] = useState([])
  
  // Estado global de UI
  const [errorMsg, setErrorMsg] = useState('')
  const [successMsg, setSuccessMsg] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [checkoutSuccess, setCheckoutSuccess] = useState(null) // Contendrá la venta completada

  // Verificar la expiración del token
  useEffect(() => {
    if (token) {
      const expiration = localStorage.getItem('tokenExpiration')
      if (expiration) {
        const expTime = new Date(expiration).getTime()
        const now = new Date().getTime()
        if (now >= expTime) {
          handleLogout()
          setErrorMsg('Your session has expired. Please log in again.')
        } else {
          // Configurar timeout para auto-logout al expirar
          const timeout = setTimeout(() => {
            handleLogout()
            setErrorMsg('Your session has expired.')
          }, expTime - now)
          return () => clearTimeout(timeout)
        }
      }
    }
  }, [token])

  // Cargar datos al autenticarse
  useEffect(() => {
    if (token && user) {
      checkClientProfile()
      fetchProducts()
    }
  }, [token, user])

  // Cargar historial cuando cambie de pestaña a mis compras
  useEffect(() => {
    if (token && activeTab === 'history' && clientId) {
      fetchSalesHistory()
    }
  }, [token, activeTab, clientId])

  // Buscar si existe el perfil de cliente para este usuario
  const checkClientProfile = async () => {
    setIsLoading(true)
    setErrorMsg('')
    try {
      const res = await fetch(`${API_BASE_URL}/clients`, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      })
      if (!res.ok) {
        if (res.status === 401) handleLogout()
        throw new Error('Error querying clients.')
      }
      const data = await res.json()
      const matched = data.find(c => c.email.toLowerCase() === user.email.toLowerCase())
      
      if (matched) {
        setClientId(matched.id)
        setClientProfile(matched)
        localStorage.setItem('clientId', matched.id)
        setNeedsProfileSetup(false)
      } else {
        setNeedsProfileSetup(true)
      }
    } catch (err) {
      setErrorMsg(err.message)
    } finally {
      setIsLoading(false)
    }
  }

  // Crear perfil de cliente en la API
  const handleCreateProfile = async (e) => {
    e.preventDefault()
    setIsLoading(true)
    setErrorMsg('')
    
    const ageNum = parseInt(profileForm.age)
    if (isNaN(ageNum) || ageNum <= 0) {
      setErrorMsg('Age must be a valid integer.')
      setIsLoading(false)
      return
    }

    try {
      const res = await fetch(`${API_BASE_URL}/clients`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          firstName: profileForm.firstName,
          lastName: profileForm.lastName,
          documentType: profileForm.documentType,
          documentNumber: profileForm.documentNumber,
          email: user.email,
          phone: profileForm.phone,
          address: profileForm.address,
          age: ageNum
        })
      })

      if (!res.ok) {
        const errorText = await res.text()
        throw new Error(errorText || 'Error saving client details.')
      }

      const newClient = await res.json()
      setClientId(newClient.id)
      setClientProfile(newClient)
      localStorage.setItem('clientId', newClient.id)
      setNeedsProfileSetup(false)
      setSuccessMsg('Profile configured successfully! Welcome to Firmeza.')
    } catch (err) {
      setErrorMsg(err.message)
    } finally {
      setIsLoading(false)
    }
  }

  // Registro de usuario en API
  const handleRegister = async (e) => {
    e.preventDefault()
    setIsLoading(true)
    setErrorMsg('')
    setSuccessMsg('')

    try {
      const res = await fetch(`${API_BASE_URL}/auth/register`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          email: regEmail,
          password: regPassword,
          displayName: regName,
          role: 'Cliente'
        })
      })

      if (!res.ok) {
        const errorData = await res.json()
        const errMsg = Array.isArray(errorData) ? errorData.map(e => e.description).join(', ') : errorData;
        throw new Error(errMsg || 'Registration error.')
      }

      const data = await res.json()
      saveAuthSession(data)
      setSuccessMsg('Registration successful.')
    } catch (err) {
      setErrorMsg(err.message)
    } finally {
      setIsLoading(false)
    }
  }

  // Inicio de sesión en API
  const handleLogin = async (e) => {
    e.preventDefault()
    setIsLoading(true)
    setErrorMsg('')
    setSuccessMsg('')

    try {
      const res = await fetch(`${API_BASE_URL}/auth/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          email: loginEmail,
          password: loginPassword
        })
      })

      if (!res.ok) {
        const errorText = await res.text()
        throw new Error(errorText || 'Invalid credentials.')
      }

      const data = await res.json()
      // Validar si cuenta con rol Cliente
      if (!data.roles.includes('Cliente') && !data.roles.includes('Admin')) {
        throw new Error('Only authorized clients can access the shopping portal.')
      }

      saveAuthSession(data)
    } catch (err) {
      setErrorMsg(err.message)
    } finally {
      setIsLoading(false)
    }
  }

  // Guardar datos de sesión
  const saveAuthSession = (data) => {
    setToken(data.token)
    setUser({ email: data.email, displayName: data.displayName, roles: data.roles })
    
    localStorage.setItem('token', data.token)
    localStorage.setItem('user', JSON.stringify({ email: data.email, displayName: data.displayName, roles: data.roles }))
    localStorage.setItem('tokenExpiration', data.expiration)
  }

  // Cerrar sesión
  const handleLogout = () => {
    setToken('')
    setUser(null)
    setClientId('')
    setClientProfile(null)
    setCart([])
    setSalesHistory([])
    setNeedsProfileSetup(false)
    
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    localStorage.removeItem('clientId')
    localStorage.removeItem('tokenExpiration')
  }

  // Obtener productos
  const fetchProducts = async () => {
    try {
      const res = await fetch(`${API_BASE_URL}/products`)
      if (!res.ok) throw new Error('Error al cargar productos.')
      const data = await res.json()
      setProducts(data)
      
      // Obtener categorías únicas
      const cats = ['Todos', ...new Set(data.map(p => p.category))]
      setCategories(cats)
    } catch (err) {
      setErrorMsg(err.message)
    }
  }

  // Obtener historial de ventas
  const fetchSalesHistory = async () => {
    try {
      const res = await fetch(`${API_BASE_URL}/sales`, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      })
      if (!res.ok) throw new Error('Error al cargar historial.')
      const data = await res.json()
      // Filtrar compras del cliente actual
      const mySales = data.filter(s => s.clientId === parseInt(clientId))
      setSalesHistory(mySales.sort((a, b) => b.id - a.id))
    } catch (err) {
      setErrorMsg(err.message)
    }
  }

  // Agregar al carrito
  const addToCart = (product) => {
    if (product.stock <= 0) return

    setCart(prev => {
      const existing = prev.find(item => item.id === product.id)
      const currentQty = existing ? existing.quantity : 0
      
      if (currentQty >= product.stock) {
        setErrorMsg(`No hay más stock disponible para ${product.name}.`)
        return prev
      }

      if (existing) {
        return prev.map(item => 
          item.id === product.id ? { ...item, quantity: item.quantity + 1 } : item
        )
      } else {
        return [...prev, { ...product, quantity: 1 }]
      }
    })
  }

  // Modificar cantidades en el carrito
  const updateCartQty = (productId, newQty, maxStock) => {
    if (newQty <= 0) {
      setCart(prev => prev.filter(item => item.id !== productId))
      return
    }
    if (newQty > maxStock) {
      setErrorMsg('Cantidad máxima excedida para este producto.')
      return
    }
    setCart(prev => prev.map(item => 
      item.id === productId ? { ...item, quantity: newQty } : item
    ))
  }

  // Cálculos del Carrito
  const cartSubtotal = cart.reduce((sum, item) => sum + (item.price * item.quantity), 0)
  const cartTax = Math.round(cartSubtotal * 0.19 * 100) / 100
  const cartTotal = cartSubtotal + cartTax

  // Finalizar Compra
  const handleCheckout = async () => {
    if (cart.length === 0) return
    setIsLoading(true)
    setErrorMsg('')
    setSuccessMsg('')

    try {
      const details = cart.map(item => ({
        productId: item.id,
        quantity: item.quantity
      }))

      const res = await fetch(`${API_BASE_URL}/sales`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          clientId: parseInt(clientId),
          details: details
        })
      })

      if (!res.ok) {
        const errorText = await res.text()
        throw new Error(errorText || 'Error al procesar la compra.')
      }

      const saleResult = await res.json()
      setCheckoutSuccess(saleResult)
      setCart([])
      setIsCartOpen(false)
      
      // Recargar catálogo para actualizar el stock
      fetchProducts()
    } catch (err) {
      setErrorMsg(err.message)
    } finally {
      setIsLoading(false)
    }
  }

  // Descargar comprobante PDF
  const downloadReceipt = async (saleId) => {
    try {
      const res = await fetch(`${API_BASE_URL}/sales/${saleId}/receipt`, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      })
      if (!res.ok) throw new Error('Error al descargar el PDF.')
      
      const blob = await res.blob()
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `comprobante-${String(saleId).padStart(6, '0')}.pdf`
      document.body.appendChild(a)
      a.click()
      a.remove()
      window.URL.revokeObjectURL(url)
    } catch (err) {
      setErrorMsg(err.message)
    }
  }

  // Filtrar productos
  const filteredProducts = products.filter(p => {
    const matchesSearch = p.name.toLowerCase().includes(searchQuery.toLowerCase()) || 
                          p.description.toLowerCase().includes(searchQuery.toLowerCase())
    const matchesCategory = selectedCategory === 'Todos' || p.category === selectedCategory
    return matchesSearch && matchesCategory
  })

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 font-sans selection:bg-amber-500 selection:text-slate-950">
      
      {/* Notificaciones flotantes */}
      {errorMsg && (
        <div className="fixed top-5 right-5 z-50 max-w-md bg-red-950 border border-red-500 text-red-200 px-4 py-3 rounded-lg shadow-2xl flex items-start gap-3 animate-bounce">
          <div className="flex-1 text-sm font-medium">{errorMsg}</div>
          <button onClick={() => setErrorMsg('')} className="text-red-400 hover:text-red-200 font-bold">&times;</button>
        </div>
      )}
      {successMsg && (
        <div className="fixed top-5 right-5 z-50 max-w-md bg-emerald-950 border border-emerald-500 text-emerald-200 px-4 py-3 rounded-lg shadow-2xl flex items-start gap-3">
          <div className="flex-1 text-sm font-medium">{successMsg}</div>
          <button onClick={() => setSuccessMsg('')} className="text-emerald-400 hover:text-emerald-200 font-bold">&times;</button>
        </div>
      )}

      {/* Pantalla No Autenticado */}
      {!token ? (
        <div className="min-h-screen flex items-center justify-center px-4 relative overflow-hidden bg-[radial-gradient(ellipse_at_top,_var(--tw-gradient-stops))] from-slate-900 via-slate-950 to-black">
          <div className="absolute inset-0 bg-[linear-gradient(to_right,#0f172a_1px,transparent_1px),linear-gradient(to_bottom,#0f172a_1px,transparent_1px)] bg-[size:4rem_4rem] [mask-image:radial-gradient(ellipse_60%_50%_at_50%_0%,#000_70%,transparent_100%)] opacity-30"></div>
          
          <div className="w-full max-w-md bg-slate-900/60 backdrop-blur-xl border border-slate-800 p-8 rounded-2xl shadow-2xl relative z-10">
            <div className="text-center mb-8">
              <h1 className="text-4xl font-extrabold tracking-tight text-amber-500 m-0">FIRMEZA</h1>
              <p className="text-slate-400 text-sm mt-1">Customer Portal</p>
            </div>

            {authMode === 'login' ? (
              <form onSubmit={handleLogin} className="space-y-5">
                <div>
                  <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">Email Address</label>
                  <input 
                    type="email" 
                    required 
                    value={loginEmail}
                    onChange={(e) => setLoginEmail(e.target.value)}
                    placeholder="user@email.com"
                    className="w-full px-4 py-3 rounded-lg bg-slate-950 border border-slate-850 focus:border-amber-500 focus:ring-1 focus:ring-amber-500 text-slate-100 placeholder-slate-600 focus:outline-none transition-all"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">Password</label>
                  <input 
                    type="password" 
                    required 
                    value={loginPassword}
                    onChange={(e) => setLoginPassword(e.target.value)}
                    placeholder="••••••••"
                    className="w-full px-4 py-3 rounded-lg bg-slate-950 border border-slate-850 focus:border-amber-500 focus:ring-1 focus:ring-amber-500 text-slate-100 placeholder-slate-600 focus:outline-none transition-all"
                  />
                </div>
                <button 
                  type="submit" 
                  disabled={isLoading}
                  className="w-full py-3 bg-amber-500 hover:bg-amber-600 active:scale-[0.98] disabled:opacity-50 text-slate-950 font-bold rounded-lg transition-all shadow-lg shadow-amber-500/10"
                >
                  {isLoading ? 'Logging in...' : 'Log In'}
                </button>
                <p className="text-center text-xs text-slate-500 mt-4">
                  Don't have an account?{' '}
                  <button type="button" onClick={() => setAuthMode('register')} className="text-amber-500 hover:underline">Register</button>
                </p>
              </form>
            ) : (
              <form onSubmit={handleRegister} className="space-y-5">
                <div>
                  <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">Full Name</label>
                  <input 
                    type="text" 
                    required 
                    value={regName}
                    onChange={(e) => setRegName(e.target.value)}
                    placeholder="John Doe"
                    className="w-full px-4 py-3 rounded-lg bg-slate-950 border border-slate-850 focus:border-amber-500 focus:ring-1 focus:ring-amber-500 text-slate-100 placeholder-slate-600 focus:outline-none transition-all"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">Email Address</label>
                  <input 
                    type="email" 
                    required 
                    value={regEmail}
                    onChange={(e) => setRegEmail(e.target.value)}
                    placeholder="user@email.com"
                    className="w-full px-4 py-3 rounded-lg bg-slate-950 border border-slate-850 focus:border-amber-500 focus:ring-1 focus:ring-amber-500 text-slate-100 placeholder-slate-600 focus:outline-none transition-all"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">Password (Minimum 6 chars)</label>
                  <input 
                    type="password" 
                    required 
                    value={regPassword}
                    onChange={(e) => setRegPassword(e.target.value)}
                    placeholder="••••••••"
                    className="w-full px-4 py-3 rounded-lg bg-slate-950 border border-slate-850 focus:border-amber-500 focus:ring-1 focus:ring-amber-500 text-slate-100 placeholder-slate-600 focus:outline-none transition-all"
                  />
                </div>
                <button 
                  type="submit" 
                  disabled={isLoading}
                  className="w-full py-3 bg-amber-500 hover:bg-amber-600 active:scale-[0.98] disabled:opacity-50 text-slate-950 font-bold rounded-lg transition-all shadow-lg shadow-amber-500/10"
                >
                  {isLoading ? 'Registering...' : 'Register'}
                </button>
                <p className="text-center text-xs text-slate-500 mt-4">
                  Already have an account?{' '}
                  <button type="button" onClick={() => setAuthMode('login')} className="text-amber-500 hover:underline">Log In</button>
                </p>
              </form>
            )}
          </div>
        </div>
      ) : needsProfileSetup ? (
        /* Pantalla Completar Perfil de Cliente */
        <div className="min-h-screen flex items-center justify-center px-4 bg-slate-950 text-slate-100 py-10">
          <div className="w-full max-w-xl bg-slate-900 border border-slate-800 p-8 rounded-2xl shadow-2xl">
            <div className="text-center mb-6">
              <h2 className="text-2xl font-bold text-amber-500">Complete Your Profile</h2>
              <p className="text-slate-400 text-sm mt-1">To make purchases and receive your receipts, we need to associate your customer details.</p>
            </div>
            
            <form onSubmit={handleCreateProfile} className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">First Name</label>
                  <input 
                    type="text" 
                    required 
                    value={profileForm.firstName}
                    onChange={(e) => setProfileForm({...profileForm, firstName: e.target.value})}
                    placeholder="John"
                    className="w-full px-4 py-2.5 rounded-lg bg-slate-950 border border-slate-800 focus:border-amber-500 focus:outline-none text-sm"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">Last Name</label>
                  <input 
                    type="text" 
                    required 
                    value={profileForm.lastName}
                    onChange={(e) => setProfileForm({...profileForm, lastName: e.target.value})}
                    placeholder="Doe"
                    className="w-full px-4 py-2.5 rounded-lg bg-slate-950 border border-slate-800 focus:border-amber-500 focus:outline-none text-sm"
                  />
                </div>
              </div>

              <div className="grid grid-cols-3 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">Doc. Type</label>
                  <select 
                    value={profileForm.documentType}
                    onChange={(e) => setProfileForm({...profileForm, documentType: e.target.value})}
                    className="w-full px-4 py-2.5 rounded-lg bg-slate-950 border border-slate-800 focus:border-amber-500 focus:outline-none text-sm text-slate-300"
                  >
                    <option value="CC">ID Card (CC)</option>
                    <option value="NIT">Tax ID (NIT)</option>
                    <option value="CE">Foreign ID (CE)</option>
                  </select>
                </div>
                <div className="col-span-2">
                  <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">Document Number</label>
                  <input 
                    type="text" 
                    required 
                    value={profileForm.documentNumber}
                    onChange={(e) => setProfileForm({...profileForm, documentNumber: e.target.value})}
                    placeholder="12345678"
                    className="w-full px-4 py-2.5 rounded-lg bg-slate-950 border border-slate-800 focus:border-amber-500 focus:outline-none text-sm"
                  />
                </div>
              </div>

              <div className="grid grid-cols-3 gap-4">
                <div className="col-span-2">
                  <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">Phone</label>
                  <input 
                    type="text" 
                    required 
                    value={profileForm.phone}
                    onChange={(e) => setProfileForm({...profileForm, phone: e.target.value})}
                    placeholder="3001234567"
                    className="w-full px-4 py-2.5 rounded-lg bg-slate-950 border border-slate-800 focus:border-amber-500 focus:outline-none text-sm"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">Age</label>
                  <input 
                    type="number" 
                    required 
                    value={profileForm.age}
                    onChange={(e) => setProfileForm({...profileForm, age: e.target.value})}
                    placeholder="30"
                    min="18"
                    max="120"
                    className="w-full px-4 py-2.5 rounded-lg bg-slate-950 border border-slate-800 focus:border-amber-500 focus:outline-none text-sm"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-2">Shipping Address</label>
                <input 
                  type="text" 
                  required 
                  value={profileForm.address}
                  onChange={(e) => setProfileForm({...profileForm, address: e.target.value})}
                  placeholder="Street 123 #45-67"
                  className="w-full px-4 py-2.5 rounded-lg bg-slate-950 border border-slate-800 focus:border-amber-500 focus:outline-none text-sm"
                />
              </div>

              <div className="pt-2">
                <button 
                  type="submit" 
                  disabled={isLoading}
                  className="w-full py-3 bg-amber-500 hover:bg-amber-600 disabled:opacity-50 text-slate-950 font-bold rounded-lg transition-all text-sm"
                >
                  {isLoading ? 'Registering...' : 'Complete Registration'}
                </button>
              </div>
            </form>
          </div>
        </div>
      ) : (
        /* Pantalla Principal Logueado */
        <div className="flex flex-col min-h-screen">
          {/* Header / Navbar */}
          <header className="sticky top-0 z-40 bg-slate-950/80 backdrop-blur-md border-b border-slate-900 px-6 py-4">
            <div className="max-w-7xl mx-auto flex items-center justify-between">
              <div className="flex items-center gap-6">
                <h1 className="text-2xl font-black text-amber-500 tracking-wider m-0">FIRMEZA</h1>
                <nav className="flex items-center gap-4">
                  <button 
                    onClick={() => { setActiveTab('catalog'); setCheckoutSuccess(null); }}
                    className={`px-3 py-1.5 rounded-lg text-sm font-semibold transition-all ${activeTab === 'catalog' ? 'bg-slate-900 text-amber-500 border border-slate-800' : 'text-slate-400 hover:text-slate-200'}`}
                  >
                    Catalog
                  </button>
                  <button 
                    onClick={() => { setActiveTab('history'); setCheckoutSuccess(null); }}
                    className={`px-3 py-1.5 rounded-lg text-sm font-semibold transition-all ${activeTab === 'history' ? 'bg-slate-900 text-amber-500 border border-slate-800' : 'text-slate-400 hover:text-slate-200'}`}
                  >
                    My Purchases
                  </button>
                </nav>
              </div>

              <div className="flex items-center gap-4">
                <div className="flex items-center gap-2 text-slate-400 text-sm bg-slate-900 border border-slate-850 px-3 py-1.5 rounded-lg">
                  <User className="w-4 h-4 text-amber-500" />
                  <span>{user.displayName}</span>
                </div>
                
                <button 
                  onClick={() => setIsCartOpen(true)}
                  className="relative p-2 bg-slate-900 hover:bg-slate-850 border border-slate-800 rounded-lg text-slate-200 hover:text-amber-500 transition-all"
                >
                  <ShoppingCart className="w-5 h-5" />
                  {cart.length > 0 && (
                    <span className="absolute -top-1.5 -right-1.5 w-5 h-5 bg-amber-500 text-slate-950 font-bold text-xs rounded-full flex items-center justify-center">
                      {cart.reduce((sum, item) => sum + item.quantity, 0)}
                    </span>
                  )}
                </button>

                <button 
                  onClick={handleLogout}
                  className="p-2 bg-slate-900/50 hover:bg-red-950 border border-slate-900 hover:border-red-900 rounded-lg text-slate-400 hover:text-red-200 transition-all"
                  title="Log Out"
                >
                  <LogOut className="w-5 h-5" />
                </button>
              </div>
            </div>
          </header>

          {/* Contenido */}
          <main className="flex-1 max-w-7xl mx-auto w-full px-6 py-8">
            
            {checkoutSuccess ? (
              /* Vista Exito Venta */
              <div className="max-w-xl mx-auto bg-slate-900 border border-slate-800 rounded-2xl p-8 text-center shadow-2xl my-10">
                <div className="w-16 h-16 bg-emerald-950 border border-emerald-500 text-emerald-400 rounded-full flex items-center justify-center mx-auto mb-6">
                  <CheckCircle className="w-10 h-10" />
                </div>
                <h2 className="text-3xl font-extrabold text-white mb-2">Purchase Completed!</h2>
                <p className="text-slate-400 text-sm mb-6">
                  The transaction has been processed securely. The official PDF receipt has been sent to your email{' '}
                  <span className="text-amber-500 font-semibold">{clientProfile?.email}</span>.
                </p>

                <div className="bg-slate-950 border border-slate-900 rounded-xl p-4 mb-8 text-left space-y-2">
                  <div className="flex justify-between text-xs text-slate-500">
                    <span>Receipt No:</span>
                    <span className="font-mono text-slate-300">#{String(checkoutSuccess.id).padStart(6, '0')}</span>
                  </div>
                  <div className="flex justify-between text-xs text-slate-500">
                    <span>Date:</span>
                    <span className="text-slate-300">{new Date(checkoutSuccess.saleDate).toLocaleDateString()}</span>
                  </div>
                  <div className="flex justify-between text-sm font-semibold border-t border-slate-900 pt-2 text-slate-300">
                    <span>Total Paid:</span>
                    <span className="text-amber-500">${checkoutSuccess.total.toFixed(2)}</span>
                  </div>
                </div>

                <div className="flex flex-col sm:flex-row gap-3 justify-center">
                  <button 
                    onClick={() => downloadReceipt(checkoutSuccess.id)}
                    className="flex items-center justify-center gap-2 px-5 py-3 bg-amber-500 hover:bg-amber-600 text-slate-950 font-bold rounded-lg transition-all"
                  >
                    <Download className="w-4 h-4" />
                    Download PDF
                  </button>
                  <button 
                    onClick={() => setCheckoutSuccess(null)}
                    className="flex items-center justify-center gap-2 px-5 py-3 bg-slate-800 hover:bg-slate-750 text-slate-200 font-bold rounded-lg transition-all"
                  >
                    Back to Catalog
                  </button>
                </div>
              </div>
            ) : activeTab === 'catalog' ? (
              /* Vista Catálogo */
              <div>
                {/* Banner de búsqueda y filtros */}
                <div className="flex flex-col md:flex-row gap-4 justify-between items-center mb-8">
                  <div className="relative w-full md:max-w-md">
                    <Search className="w-5 h-5 absolute left-3 top-3.5 text-slate-650" />
                    <input 
                      type="text" 
                      placeholder="Search cement, reinforcing bars, bricks..."
                      value={searchQuery}
                      onChange={(e) => setSearchQuery(e.target.value)}
                      className="w-full pl-10 pr-4 py-3 bg-slate-900 border border-slate-850 rounded-xl focus:border-amber-500 focus:ring-1 focus:ring-amber-500 text-slate-200 placeholder-slate-600 focus:outline-none text-sm transition-all"
                    />
                  </div>

                  {/* Filtro categorías */}
                  <div className="flex gap-2 overflow-x-auto w-full md:w-auto pb-2 md:pb-0">
                    {categories.map(cat => (
                      <button
                        key={cat}
                        onClick={() => setSelectedCategory(cat)}
                        className={`px-4 py-2 rounded-xl text-xs font-bold whitespace-nowrap transition-all border ${selectedCategory === cat ? 'bg-amber-500 border-amber-500 text-slate-950 shadow-lg shadow-amber-500/10' : 'bg-slate-900 border-slate-850 text-slate-400 hover:text-slate-200'}`}
                      >
                        {cat}
                      </button>
                    ))}
                  </div>
                </div>

                {/* Grid de Productos */}
                {filteredProducts.length === 0 ? (
                  <div className="text-center py-20 bg-slate-900/30 border border-dashed border-slate-900 rounded-2xl">
                    <ShoppingBag className="w-12 h-12 text-slate-700 mx-auto mb-4" />
                    <h3 className="text-lg font-bold text-slate-400">No products found</h3>
                    <p className="text-slate-600 text-sm mt-1">Try changing the search terms or filter.</p>
                  </div>
                ) : (
                  <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
                    {filteredProducts.map(p => (
                      <div key={p.id} className="bg-slate-900 border border-slate-850 rounded-xl p-5 flex flex-col justify-between hover:border-slate-700 hover:shadow-xl transition-all group">
                        <div>
                          <div className="flex justify-between items-start gap-2 mb-2">
                            <span className="text-[10px] uppercase tracking-wider font-extrabold text-amber-500 px-2 py-0.5 bg-amber-500/10 rounded-full">{p.category}</span>
                            
                            {p.stock <= 0 ? (
                              <span className="text-[10px] uppercase tracking-wider font-extrabold text-red-500 bg-red-950 px-2 py-0.5 rounded-full">Out of Stock</span>
                            ) : p.stock < 10 ? (
                              <span className="text-[10px] uppercase tracking-wider font-extrabold text-orange-500 bg-orange-950 px-2 py-0.5 rounded-full">Low Stock ({p.stock})</span>
                            ) : (
                              <span className="text-xs text-slate-500 font-medium">Stock: {p.stock} {p.unit}</span>
                            )}
                          </div>

                          <h3 className="text-lg font-bold text-white group-hover:text-amber-400 transition-colors">{p.name}</h3>
                          <p className="text-slate-400 text-xs mt-1 line-clamp-2 min-h-[2rem]">{p.description || 'No description available.'}</p>
                        </div>

                        <div className="flex items-center justify-between mt-5 pt-4 border-t border-slate-900">
                          <div>
                            <span className="block text-[10px] text-slate-650 uppercase font-bold tracking-wider">Unit Price</span>
                            <span className="text-xl font-black text-white">${p.price.toFixed(2)}</span>
                          </div>

                          <button
                            disabled={p.stock <= 0}
                            onClick={() => addToCart(p)}
                            className="px-4 py-2 rounded-lg bg-slate-800 hover:bg-amber-500 disabled:opacity-30 disabled:bg-slate-800/50 text-slate-100 group-hover:text-white hover:text-slate-950 font-bold text-xs transition-all flex items-center gap-1.5"
                          >
                            <span>Add</span>
                            <ArrowRight className="w-3.5 h-3.5 group-hover:translate-x-0.5 transition-transform" />
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            ) : (
              /* Vista Historial */
              <div className="max-w-4xl mx-auto">
                <div className="flex items-center justify-between mb-6">
                  <h2 className="text-xl font-bold text-slate-200 flex items-center gap-2">
                    <FileText className="w-5 h-5 text-amber-500" />
                    My Purchases History
                  </h2>
                </div>

                {salesHistory.length === 0 ? (
                  <div className="text-center py-20 bg-slate-900/30 border border-dashed border-slate-900 rounded-2xl">
                    <ShoppingBag className="w-12 h-12 text-slate-700 mx-auto mb-4" />
                    <h3 className="text-lg font-bold text-slate-400">You haven't made any purchases yet</h3>
                    <p className="text-slate-600 text-sm mt-1">Explore our catalog and purchase your materials directly.</p>
                  </div>
                ) : (
                  <div className="space-y-4">
                    {salesHistory.map(sale => (
                      <div key={sale.id} className="bg-slate-900 border border-slate-850 rounded-xl p-5 flex flex-col sm:flex-row justify-between sm:items-center gap-4 hover:border-slate-800 transition-all">
                        <div>
                          <div className="flex items-center gap-3">
                            <span className="font-mono text-slate-200 font-bold">No. #{String(sale.id).padStart(6, '0')}</span>
                            <span className="px-2 py-0.5 bg-emerald-500/10 text-emerald-400 font-bold text-[10px] rounded-full uppercase">Completed</span>
                          </div>
                          
                          <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-slate-500 mt-2">
                            <span>Date: {new Date(sale.saleDate).toLocaleString()}</span>
                            <span>Items: {sale.details.reduce((sum, d) => sum + d.quantity, 0)}</span>
                          </div>

                          {/* Previsualizar productos comprados */}
                          <div className="mt-2 text-xs text-slate-450 italic">
                            {sale.details.map(d => `${d.productName || 'Product'} (x${d.quantity})`).join(', ')}
                          </div>
                        </div>

                        <div className="flex items-center gap-4 justify-between border-t border-slate-850 pt-3 sm:pt-0 sm:border-t-0">
                          <div className="text-right">
                            <span className="block text-[10px] text-slate-650 uppercase font-bold tracking-wider">Total</span>
                            <span className="text-lg font-extrabold text-amber-500">${sale.total.toFixed(2)}</span>
                          </div>

                          <button 
                            onClick={() => downloadReceipt(sale.id)}
                            className="p-2.5 bg-slate-950 border border-slate-800 rounded-lg text-slate-400 hover:text-amber-500 hover:border-amber-500/30 transition-all"
                            title="Download PDF Receipt"
                          >
                            <Download className="w-4.5 h-4.5" />
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            )}

          </main>

          {/* Carrito de Compras (Slide Drawer lateral) */}
          {isCartOpen && (
            <div className="fixed inset-0 z-50 overflow-hidden flex justify-end">
              {/* Overlay */}
              <div 
                onClick={() => setIsCartOpen(false)}
                className="absolute inset-0 bg-slate-950/80 backdrop-blur-sm transition-opacity"
              ></div>

              {/* Contenedor Drawer */}
              <div className="w-full max-w-md bg-slate-900 border-l border-slate-800 relative z-10 flex flex-col h-full shadow-2xl animate-slide-left">
                {/* Header */}
                <div className="p-6 border-b border-slate-850 flex items-center justify-between">
                  <h3 className="text-lg font-bold text-white flex items-center gap-2">
                    <ShoppingCart className="w-5 h-5 text-amber-500" />
                    My Cart
                  </h3>
                  <button onClick={() => setIsCartOpen(false)} className="text-slate-400 hover:text-slate-100 font-bold text-2xl leading-none">&times;</button>
                </div>

                {/* Items */}
                <div className="flex-1 overflow-y-auto p-6 space-y-4">
                  {cart.length === 0 ? (
                    <div className="text-center py-20 text-slate-600 space-y-3">
                      <ShoppingCart className="w-12 h-12 text-slate-700 mx-auto" />
                      <p className="text-sm font-medium">Your cart is empty.</p>
                      <button onClick={() => setIsCartOpen(false)} className="text-xs text-amber-500 hover:underline">Back to Catalog</button>
                    </div>
                  ) : (
                    cart.map(item => (
                      <div key={item.id} className="flex gap-4 p-3 bg-slate-950 border border-slate-900 rounded-xl">
                        <div className="flex-1 min-w-0">
                          <h4 className="text-sm font-semibold text-white truncate">{item.name}</h4>
                          <span className="text-xs text-slate-500">${item.price.toFixed(2)} per {item.unit}</span>
                          
                          <div className="flex items-center gap-2 mt-2">
                            <button 
                              onClick={() => updateCartQty(item.id, item.quantity - 1, item.stock)}
                              className="w-7 h-7 bg-slate-900 border border-slate-800 rounded flex items-center justify-center font-bold text-slate-350 hover:bg-slate-800"
                            >-</button>
                            <span className="w-8 text-center text-xs font-semibold text-white">{item.quantity}</span>
                            <button 
                              onClick={() => updateCartQty(item.id, item.quantity + 1, item.stock)}
                              className="w-7 h-7 bg-slate-900 border border-slate-800 rounded flex items-center justify-center font-bold text-slate-350 hover:bg-slate-800"
                            >+</button>
                          </div>
                        </div>

                        <div className="flex flex-col justify-between items-end">
                          <button 
                            onClick={() => updateCartQty(item.id, 0, item.stock)}
                            className="p-1.5 text-slate-550 hover:text-red-400 hover:bg-slate-900 rounded"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                          <span className="text-sm font-bold text-slate-200">${(item.price * item.quantity).toFixed(2)}</span>
                        </div>
                      </div>
                    ))
                  )}
                </div>

                {/* Resumen & Checkout */}
                {cart.length > 0 && (
                  <div className="p-6 border-t border-slate-850 bg-slate-950 space-y-4">
                    <div className="space-y-1.5 text-xs text-slate-400">
                      <div className="flex justify-between">
                        <span>Subtotal:</span>
                        <span className="text-slate-200 font-semibold">${cartSubtotal.toFixed(2)}</span>
                      </div>
                      <div className="flex justify-between">
                        <span>VAT (19%):</span>
                        <span className="text-slate-200 font-semibold">${cartTax.toFixed(2)}</span>
                      </div>
                      <div className="flex justify-between text-sm border-t border-slate-900 pt-2 text-slate-200 font-extrabold">
                        <span>Total Purchase:</span>
                        <span className="text-amber-500">${cartTotal.toFixed(2)}</span>
                      </div>
                    </div>

                    <button 
                      onClick={handleCheckout}
                      disabled={isLoading}
                      className="w-full py-3 bg-amber-500 hover:bg-amber-600 disabled:opacity-50 text-slate-950 font-bold rounded-lg transition-all text-sm"
                    >
                      {isLoading ? 'Processing Purchase...' : 'Confirm and Pay'}
                    </button>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Footer */}
          <footer className="border-t border-slate-900/60 bg-slate-950 py-6 text-center text-slate-500 text-xs mt-auto">
            <p>Firmeza © — Customer Shopping & Self-Service Portal</p>
          </footer>
        </div>
      )}
    </div>
  )
}

export default App

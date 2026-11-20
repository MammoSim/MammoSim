// Import the functions you need from the SDKs you need
import { initializeApp } from "firebase/app";
import {getAuth} from "firebase/auth";
// TODO: Add SDKs for Firebase products that you want to use
// https://firebase.google.com/docs/web/setup#available-libraries

// Your web app's Firebase configuration
const firebaseConfig = {
  apiKey: "AIzaSyCSXKM5W2gbk0hcaE0kVqlaT2CdRL0tk4I",
  authDomain: "mammosim.firebaseapp.com",
  projectId: "mammosim",
  storageBucket: "mammosim.firebasestorage.app",
  messagingSenderId: "73123150298",
  appId: "1:73123150298:web:a53f25215c00c86d2a28e3"
};


// Initialize Firebase
const app = initializeApp(firebaseConfig);
export const auth = getAuth(app);
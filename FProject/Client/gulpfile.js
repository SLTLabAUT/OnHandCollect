const gulp = require("gulp");
const del = require("del");
const sourcemaps = require("gulp-sourcemaps");
const terser = require("gulp-terser");
const ts = require("gulp-typescript");
const pathResolver = require("gulp-typescript-path-resolver");
const sass = require("gulp-sass")(require('sass'));
const preprocess = require("gulp-preprocess");
const rename = require("gulp-rename");

require("dotenv").config({ path: "../../.env" });

const launchSettings = require("./Properties/launchSettings.json");
const tsProject = ts.createProject("tsconfig.json");

process.env.ASPNETCORE_ENVIRONMENT =
    process.env.ASPNETCORE_ENVIRONMENT
    || (launchSettings && launchSettings.profiles["IIS Express"].environmentVariables.ASPNETCORE_ENVIRONMENT);

function clean() {
    return del([
        "wwwroot/ts/**/*.js",
        "wwwroot/ts/**/*.js.map",
        "wwwroot/sass/**/*.css",
        "wwwroot/sass/**/*.css.map",
        "Pages/**/*.js",
        "Pages/**/*.css",
        "Pages/**/*.js.map",
        "Pages/**/*.css.map",
        "Shared/**/*.js",
        "Shared/**/*.css",
        "Shared/**/*.js.map",
        "Shared/**/*.css.map",
    ]);
}

function buildSass() {
    return gulp.src([
            "Pages/**/*.scss",
            "Shared/**/*.scss",
            "wwwroot/sass/**/*.scss"
        ], { base: "./" })
        .pipe(sass().on("error", sass.logError))
        .pipe(gulp.dest("."));
}

function buildTs() {
    return tsProject.src()
        .pipe(sourcemaps.init())
        .pipe(tsProject())
        .pipe(pathResolver.tsPathResolver(tsProject.config.compilerOptions, {}))
        .pipe(terser())
        //.pipe(rename(function (path) {
        //    // Updates the object in-place
        //    path.basename = path.basename.replace(/\.(?:razor|cshtml)$/, "");
        //}))
        .pipe(gulp.dest(".", { sourcemaps: true }));
}

function release() {
    return gulp.src("wwwroot/index.extended.html")
        .pipe(preprocess())
        .pipe(rename("wwwroot/index.html"))
        .pipe(gulp.dest("."));
}

exports.default = gulp.series(clean, gulp.parallel(buildSass, buildTs, release));
exports.buildTs = buildTs;
exports.buildSass = buildSass;
exports.release = release;
exports.clean = clean;
